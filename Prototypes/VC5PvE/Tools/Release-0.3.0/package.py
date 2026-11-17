"""Package the Windows build for the Shared AP v0.3.0 preview."""
import hashlib
import json
import pathlib
import re
import subprocess
import zipfile


ROOT = pathlib.Path(__file__).resolve().parents[2]
TASK_DIR = pathlib.Path(__file__).resolve().parent
OUT = ROOT / "Builds" / "Release-0.3.0"
WINDOWS = OUT / "Windows"
EXE_NAME = "VC5PvE-SharedAP.exe"
ZIP_NAME = "VC5PvE-SharedAP-0.3.0-Windows-x64.zip"
TAG = "pve-v0.3.0-shared-ap-20261117"
VERSION = "0.3.0"


def read_source_commit():
    path = OUT / "source-commit.txt"
    if not path.is_file():
        raise SystemExit("Missing Builds/Release-0.3.0/source-commit.txt.")
    commit = path.read_text(encoding="utf-8-sig").strip()
    if not re.fullmatch(r"[0-9a-fA-F]{40}", commit):
        raise SystemExit("source-commit.txt must contain one full 40-character commit SHA.")
    current = subprocess.run(
        ["git", "rev-parse", "HEAD"], cwd=ROOT, capture_output=True, text=True, check=True
    ).stdout.strip()
    if current.lower() != commit.lower():
        raise SystemExit("HEAD does not match Builds/Release-0.3.0/source-commit.txt.")
    return commit.lower()


def write_new(path, data):
    with path.open("x", encoding="utf-8", newline="\n") as stream:
        stream.write(data)


def main():
    source_commit = read_source_commit()
    required = (
        WINDOWS / EXE_NAME,
        WINDOWS / "UnityPlayer.dll",
        WINDOWS / "VC5PvE-SharedAP_Data" / "globalgamemanagers",
        WINDOWS / "MonoBleedingEdge",
    )
    for path in required:
        if not path.exists():
            raise SystemExit("Required Windows build file is missing: " + str(path))

    notice_path = ROOT / "THIRD_PARTY_NOTICES.md"
    if not notice_path.is_file():
        raise SystemExit("Missing THIRD_PARTY_NOTICES.md.")
    notice = notice_path.read_text(encoding="utf-8-sig")
    license_paths = sorted(ROOT.glob("Assets/**/LICENSE.txt"))
    license_paths += sorted(ROOT.glob("Assets/**/OFL.txt"))
    for path in license_paths:
        notice += "\n\n---\n" + str(path.relative_to(ROOT)) + "\n\n"
        notice += path.read_text(encoding="utf-8-sig")

    licenses_out = OUT / "THIRD-PARTY-LICENSES.txt"
    write_new(licenses_out, notice)
    shutil_target = WINDOWS / "THIRD-PARTY-LICENSES.txt"
    write_new(shutil_target, notice)
    write_new(
        WINDOWS / "试玩说明.txt",
        "晴岚战术 v0.3.0｜共享 AP 单关试玩版\n\n"
        "运行 VC5PvE-SharedAP.exe 开始晴岚单关。\n"
        "本版使用共享 AP，并包含固定教程；按 D5 可重新开始且跳过教程。\n"
        "本版为单关试玩，不含个人 AP 三关流程。\n"
        "源码提交：" + source_commit + "\n",
    )
    info = {
        "version": VERSION,
        "source_commit": source_commit,
        "tag": TAG,
        "target": "Windows-x64",
        "ap": "shared",
        "content": "single-stage Shared AP Qinglan encounter",
        "tutorial": "fixed tutorial; D5 restart skips tutorial",
        "scene": "Assets/VC5PvE/SharedAP/Scenes/SharedAP_Qinglan.unity",
    }
    write_new(
        WINDOWS / "build-info.json",
        json.dumps(info, ensure_ascii=False, indent=2) + "\n",
    )

    archive = OUT / ZIP_NAME
    with zipfile.ZipFile(archive, "x", zipfile.ZIP_DEFLATED, compresslevel=6) as zf:
        for path in sorted(WINDOWS.rglob("*")):
            if path.is_file() and not any("DoNotShip" in part for part in path.parts):
                zf.write(path, str(path.relative_to(OUT)))
    with zipfile.ZipFile(archive) as zf:
        if zf.testzip() is not None:
            raise SystemExit("Windows ZIP integrity check failed.")
        if any("DoNotShip" in name for name in zf.namelist()):
            raise SystemExit("Windows ZIP contains a DoNotShip path.")

    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    print(json.dumps({"file": str(archive), "bytes": archive.stat().st_size, "sha256": digest}, ensure_ascii=False))


if __name__ == "__main__":
    main()
