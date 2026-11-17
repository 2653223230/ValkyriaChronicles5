"""Verify the Android ARM64 IL2CPP Shared AP v0.3.0 APK and hash artifacts."""
import hashlib
import json
import os
import pathlib
import subprocess
import zipfile


ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "Builds" / "Release-0.3.0"
APK_NAME = "VC5PvE-SharedAP-0.3.0-Android-arm64.apk"
ANDROID_PLAYER = pathlib.Path(
    os.environ.get(
        "VC5_ANDROID_PLAYER",
        "D:/unity/安装位置/2021.3.33f1c1/Editor/Data/PlaybackEngines/AndroidPlayer",
    )
)
BUILD_TOOLS = pathlib.Path(os.environ.get("VC5_ANDROID_BUILD_TOOLS", str(ANDROID_PLAYER / "SDK/build-tools/30.0.2")))
TEMP_DIR = ROOT / ".cache" / "release-0.3.0" / "tmp"
VERSION = "0.3.0"
PACKAGE = "com.vc5pve.sharedap"


def run_checked(args, env):
    result = subprocess.run(args, capture_output=True, check=True, env=env)
    return result.stdout.decode("utf-8", errors="replace")


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main():
    apk = OUT / APK_NAME
    if not apk.is_file():
        raise SystemExit("Missing Android APK: " + str(apk))
    TEMP_DIR.mkdir(parents=True, exist_ok=True)
    env = dict(
        os.environ,
        JAVA_HOME=str(ANDROID_PLAYER / "OpenJDK"),
        TEMP=str(TEMP_DIR),
        TMP=str(TEMP_DIR),
        TMPDIR=str(TEMP_DIR),
    )
    java = ANDROID_PLAYER / "OpenJDK/bin/java.exe"
    aapt = BUILD_TOOLS / "aapt.exe"
    apksigner = BUILD_TOOLS / "lib/apksigner.jar"
    for tool in (java, aapt, apksigner):
        if not tool.is_file():
            raise SystemExit("Android verification tool is missing: " + str(tool))

    with zipfile.ZipFile(apk) as zf:
        bad_entry = zf.testzip()
        if bad_entry is not None:
            raise SystemExit("APK ZIP integrity check failed at: " + bad_entry)
        names = zf.namelist()
        abis = sorted({name.split("/")[1] for name in names if name.startswith("lib/") and len(name.split("/")) > 2})
        if abis != ["arm64-v8a"]:
            raise SystemExit("Expected only arm64-v8a libraries, found: " + repr(abis))
        if "lib/arm64-v8a/libil2cpp.so" not in names:
            raise SystemExit("APK does not contain the ARM64 IL2CPP library.")
        if "assets/bin/Data/level0" not in names:
            raise SystemExit("APK is missing the single startup scene data (level0).")
        if "assets/bin/Data/level1" in names:
            raise SystemExit("APK contains an unexpected second scene (level1).")

    badging = run_checked([str(aapt), "dump", "badging", str(apk)], env)
    expected = (
        "name='" + PACKAGE + "'",
        "versionCode='3'",
        "versionName='" + VERSION + "'",
        "sdkVersion:'24'",
        "targetSdkVersion:'33'",
        "native-code: 'arm64-v8a'",
    )
    for item in expected:
        if item not in badging:
            raise SystemExit("APK manifest does not match expected setting: " + item)
    (OUT / "Android-manifest-verification.txt").write_text(badging, encoding="utf-8", newline="\n")

    signature = run_checked(
        [str(java), "-Djava.io.tmpdir=" + str(TEMP_DIR), "-jar", str(apksigner), "verify", "--verbose", "--print-certs", str(apk)],
        env,
    )
    if "Verified using v2 scheme (APK Signature Scheme v2): true" not in signature:
        raise SystemExit("APK v2 signature verification failed.")
    (OUT / "Android-signature-verification.txt").write_text(signature, encoding="utf-8", newline="\n")

    artifacts = {}
    for name in ("VC5PvE-SharedAP-0.3.0-Windows-x64.zip", APK_NAME, "THIRD-PARTY-LICENSES.txt"):
        path = OUT / name
        if not path.is_file():
            raise SystemExit("Missing artifact for SHA256SUMS: " + str(path))
        artifacts[name] = {"bytes": path.stat().st_size, "sha256": sha256(path)}
    sums_path = OUT / "SHA256SUMS.txt"
    sums = "".join(info["sha256"] + "  " + name + "\n" for name, info in artifacts.items())
    with sums_path.open("x", encoding="utf-8", newline="\n") as stream:
        stream.write(sums)
    (OUT / "artifact-verification.json").write_text(
        json.dumps({"package": PACKAGE, "version": VERSION, "abis": abis, "scenes": ["level0"], "artifacts": artifacts}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
        newline="\n",
    )
    print(json.dumps(artifacts, ensure_ascii=False))


if __name__ == "__main__":
    main()
