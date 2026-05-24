#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
将文件夹中的图片按文件名顺序拼成视频；每张展示时长相同，片间使用随机转场。

依赖：本机已安装 ffmpeg 且在 PATH 中可用。
"""

from __future__ import annotations

import argparse
import random
import re
import subprocess
import sys
from pathlib import Path


IMAGE_EXTENSIONS = {
    ".jpg",
    ".jpeg",
    ".png",
    ".bmp",
    ".webp",
    ".tif",
    ".tiff",
    ".gif",
}

# 优先尝试的视频编码器（按顺序匹配 `ffmpeg -encoders` 中是否存在）
VIDEO_ENCODER_CANDIDATES = [
    "libx264",
    "h264_mf",  # Windows 常用，系统自带媒体基础
    "h264_nvenc",
    "h264_amf",
    "h264_qsv",
    "libopenh264",
    "mpeg4",  # 非 H.264，兼容性兜底
]

# ffmpeg xfade 支持的 transition 名称（较常见的一批；可按需增删）
XFADE_TRANSITIONS = [
    "fade",
    "fadeblack",
    "fadewhite",
    "wipeleft",
    "wiperight",
    "wipeup",
    "wipedown",
    "slideleft",
    "slideright",
    "slideup",
    "slidedown",
    "circleopen",
    "circleclose",
    "radial",
    "smoothleft",
    "smoothright",
    "smoothup",
    "smoothdown",
    "diagtl",
    "diagtr",
    "diagbl",
    "diagbr",
    "hlslice",
    "hrslice",
    "vuslice",
    "vdslice",
    "squeezeh",
    "squeezev",
    "zoomin",
]


def natural_sort_key(path: Path):
    """文件名自然排序（1, 2, 10）。"""
    name = path.name
    return [int(t) if t.isdigit() else t.lower() for t in re.split(r"(\d+)", name)]


def collect_images(folder: Path) -> list[Path]:
    paths = []
    for p in folder.iterdir():
        if not p.is_file():
            continue
        if p.suffix.lower() in IMAGE_EXTENSIONS:
            paths.append(p)
    paths.sort(key=natural_sort_key)
    return paths


def ffmpeg_bin() -> str:
    return "ffmpeg.exe" if sys.platform == "win32" else "ffmpeg"


def collect_available_video_encoders(ff: str) -> set[str]:
    """解析 `ffmpeg -encoders` 输出中的视频编码器名称。"""
    proc = subprocess.run(
        [ff, "-hide_banner", "-encoders"],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    text = (proc.stdout or "") + "\n" + (proc.stderr or "")
    names: set[str] = set()
    for raw in text.splitlines():
        line = raw.strip()
        if not line or line.startswith("------"):
            continue
        parts = line.split()
        if len(parts) < 2:
            continue
        flags, name = parts[0], parts[1]
        # 形如 V..... / V....D，首字符 V 表示视频编码器
        if flags and flags[0] == "V":
            names.add(name)
    return names


def resolve_video_encoder(ff: str, override: str | None) -> str:
    avail = collect_available_video_encoders(ff)
    if override:
        if override not in avail:
            sample = ", ".join(sorted(avail)[:40])
            more = "" if len(avail) <= 40 else " …"
            raise ValueError(
                f"指定的编码器不可用: {override}。本机部分可用视频编码器: {sample}{more}"
            )
        return override
    for name in VIDEO_ENCODER_CANDIDATES:
        if name in avail:
            return name
    raise RuntimeError(
        "未找到可用的视频编码器（已尝试 libx264、h264_mf、NVENC/AMF/QSV、libopenh264、mpeg4 等）。"
        "请安装完整版 ffmpeg，或使用 --encoder 指定本机已有的编码器名称。"
    )


def build_video_encode_args(
    encoder: str,
    crf: int,
    preset: str,
    video_bitrate: str,
) -> list[str]:
    """根据编码器生成 -c:v 及关联参数（非 libx264 时常用码率控制）。"""
    if encoder == "libx264":
        return [
            "-c:v",
            "libx264",
            "-pix_fmt",
            "yuv420p",
            "-crf",
            str(crf),
            "-preset",
            preset,
        ]
    if encoder == "libopenh264":
        return [
            "-c:v",
            "libopenh264",
            "-pix_fmt",
            "yuv420p",
            "-b:v",
            video_bitrate,
        ]
    if encoder == "mpeg4":
        q = max(2, min(31, int(crf)))
        return ["-c:v", "mpeg4", "-pix_fmt", "yuv420p", "-q:v", str(q)]
    if encoder in ("h264_mf", "h264_nvenc", "h264_amf", "h264_qsv"):
        args = ["-c:v", encoder, "-pix_fmt", "yuv420p", "-b:v", video_bitrate]
        if encoder == "h264_nvenc":
            args.extend(["-preset", "p4"])
        return args
    # 其它未知名称：通用码率模式
    return ["-c:v", encoder, "-pix_fmt", "yuv420p", "-b:v", video_bitrate]


def build_scale_pad_filter(width: int, height: int) -> str:
    # 保持比例缩放到区域内，再居中 pad
    return (
        f"scale={width}:{height}:force_original_aspect_ratio=decrease,"
        f"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2,"
        f"format=yuv420p,setsar=1"
    )


def run_ffmpeg(args_list: list[str]) -> None:
    # Windows 默认会用系统代码页(如 gbk)解码子进程输出，ffmpeg 常输出非 GBK 字节导致解码崩溃
    proc = subprocess.run(
        args_list,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if proc.returncode != 0:
        err = (proc.stderr or proc.stdout or "").strip()
        raise RuntimeError(f"ffmpeg 失败 (exit {proc.returncode}):\n{err}")


def images_to_video(
    images: list[Path],
    output: Path,
    seconds_per_image: float,
    transition_seconds: float,
    width: int,
    height: int,
    fps: int,
    seed: int | None,
    crf: int,
    preset: str,
    video_encoder: str | None,
    video_bitrate: str,
    transition_pool: list[str] | None = None,
) -> str:
    if seconds_per_image <= 0:
        raise ValueError("每张展示时长必须大于 0")
    if transition_seconds <= 0:
        raise ValueError("转场时长必须大于 0")
    if transition_seconds >= seconds_per_image:
        raise ValueError("转场时长必须小于每张展示时长（否则 xfade 的 offset 无效）")
    if not images:
        raise ValueError("没有找到图片")
    if width < 2 or height < 2:
        raise ValueError("输出宽高过小")

    pool = transition_pool if transition_pool else XFADE_TRANSITIONS
    rng = random.Random(seed)

    ff = ffmpeg_bin()
    encoder = resolve_video_encoder(ff, video_encoder)
    venc = build_video_encode_args(encoder, crf, preset, video_bitrate)
    spf = seconds_per_image
    d = transition_seconds

    # 单张：直接编码
    if len(images) == 1:
        vf = build_scale_pad_filter(width, height)
        cmd = [
            ff,
            "-y",
            "-loop",
            "1",
            "-framerate",
            str(fps),
            "-t",
            str(spf),
            "-i",
            str(images[0].resolve()),
            "-vf",
            vf,
            "-r",
            str(fps),
            *venc,
            str(output.resolve()),
        ]
        run_ffmpeg(cmd)
        return encoder

    inputs: list[str] = []
    for img in images:
        inputs.extend(
            [
                "-loop",
                "1",
                "-framerate",
                str(fps),
                "-t",
                str(spf),
                "-i",
                str(img.resolve()),
            ]
        )

    scale_f = build_scale_pad_filter(width, height)
    parts: list[str] = []
    for i in range(len(images)):
        parts.append(f"[{i}:v]{scale_f}[v{i}]")

    cur = "[v0]"
    cur_label = "v0"
    for k in range(1, len(images)):
        nxt = f"v{k}"
        t = rng.choice(pool)
        offset = k * (spf - d)
        out = f"x{k}"
        parts.append(
            f"{cur}[{nxt}]xfade=transition={t}:duration={d}:offset={offset}[{out}]"
        )
        cur = f"[{out}]"
        cur_label = out

    filter_complex = ";".join(parts)

    cmd = [
        ff,
        "-y",
        *inputs,
        "-filter_complex",
        filter_complex,
        "-map",
        f"[{cur_label}]",
        "-r",
        str(fps),
        *venc,
        str(output.resolve()),
    ]
    run_ffmpeg(cmd)
    return encoder


def main() -> int:
    parser = argparse.ArgumentParser(
        description="将文件夹内图片按文件名顺序合成视频，片间随机 xfade 转场。"
    )
    parser.add_argument(
        "--folder",
        type=Path,
        default=r"./123123",
        help="图片所在文件夹",
    )
    parser.add_argument(
        "-o",
        "--output",
        type=Path,
        default=Path("slideshow.mp4"),
        help="输出视频路径（默认 slideshow.mp4）",
    )
    parser.add_argument(
        "-t",
        "--seconds-per-image",
        type=float,
        default=3.0,
        help="每张图片展示时长（秒），默认 3",
    )
    parser.add_argument(
        "-d",
        "--transition",
        type=float,
        default=1,
        help="转场时长（秒），必须小于每张展示时长，默认 0.5",
    )
    parser.add_argument(
        "--width",
        type=int,
        default=1920,
        help="输出宽度，默认 1920",
    )
    parser.add_argument(
        "--height",
        type=int,
        default=1080,
        help="输出高度，默认 1080",
    )
    parser.add_argument(
        "--fps",
        type=int,
        default=30,
        help="帧率，默认 30",
    )
    parser.add_argument(
        "--seed",
        type=int,
        default=None,
        help="随机种子（可复现同一批转场），默认不设",
    )
    parser.add_argument(
        "--crf",
        type=int,
        default=20,
        help="libx264 CRF，默认 20",
    )
    parser.add_argument(
        "--preset",
        type=str,
        default="medium",
        help="x264 preset，默认 medium",
    )
    parser.add_argument(
        "--encoder",
        type=str,
        default=None,
        help="视频编码器名称（默认自动：libx264 -> h264_mf -> … -> mpeg4）",
    )
    parser.add_argument(
        "--video-bitrate",
        type=str,
        default="8M",
        help='非 libx264 时使用的视频码率（如 5M、12M），默认 8M',
    )
    args = parser.parse_args()

    folder = args.folder.expanduser().resolve()
    if not folder.is_dir():
        print(f"错误：不是文件夹：{folder}", file=sys.stderr)
        return 1

    images = collect_images(folder)
    if not images:
        print(f"错误：{folder} 下没有支持的图片 ({', '.join(sorted(IMAGE_EXTENSIONS))})", file=sys.stderr)
        return 1

    try:
        enc = images_to_video(
            images=images,
            output=args.output.expanduser().resolve(),
            seconds_per_image=args.seconds_per_image,
            transition_seconds=args.transition,
            width=args.width,
            height=args.height,
            fps=args.fps,
            seed=args.seed,
            crf=args.crf,
            preset=args.preset,
            video_encoder=args.encoder,
            video_bitrate=args.video_bitrate,
        )
    except Exception as e:
        print(f"错误：{e}", file=sys.stderr)
        return 1

    print(
        f"完成：{len(images)} 张图 -> {args.output.resolve()}（编码器: {enc}）"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
