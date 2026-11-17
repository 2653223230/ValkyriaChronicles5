"""Create, upload to, and publish the Shared AP v0.3.0 GitHub prerelease."""
import hashlib
import json
import pathlib
import re
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request


BASE = "https://api.github.com/repos/2653223230/ValkyriaChronicles5"
UPLOAD_BASE = "https://uploads.github.com/repos/2653223230/ValkyriaChronicles5/releases/"
ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "Builds" / "Release-0.3.0"
TASK_DIR = pathlib.Path(__file__).resolve().parent
TAG = "pve-v0.3.0-shared-ap-20261117"
RELEASE_NAME = "晴岚战术 v0.3.0｜共享 AP 单关试玩版"
ASSETS = (
    "VC5PvE-SharedAP-0.3.0-Windows-x64.zip",
    "VC5PvE-SharedAP-0.3.0-Android-arm64.apk",
    "SHA256SUMS.txt",
    "THIRD-PARTY-LICENSES.txt",
)


def source_commit():
    path = OUT / "source-commit.txt"
    if not path.is_file():
        raise SystemExit("Missing Builds/Release-0.3.0/source-commit.txt.")
    value = path.read_text(encoding="utf-8-sig").strip()
    if not re.fullmatch(r"[0-9a-fA-F]{40}", value):
        raise SystemExit("source-commit.txt must contain one full 40-character commit SHA.")
    return value.lower()


def credential_token():
    try:
        result = subprocess.run(
            ["git", "credential", "fill"],
            input=b"protocol=https\nhost=github.com\n\n",
            capture_output=True,
            check=True,
        )
    except (OSError, subprocess.CalledProcessError):
        raise SystemExit("Could not obtain GitHub credentials from Git Credential Manager.")
    fields = {}
    for line in result.stdout.decode("utf-8", errors="replace").splitlines():
        if "=" in line:
            key, value = line.split("=", 1)
            fields[key] = value
    token = fields.get("password")
    if not token:
        raise SystemExit("No GitHub credential is available.")
    return token


TOKEN = None


def api(path, method="GET", body=None, binary=False, not_found_ok=False):
    global TOKEN
    if TOKEN is None:
        TOKEN = credential_token()
    if path.startswith("https://"):
        url = path
        if not url.startswith(UPLOAD_BASE):
            raise RuntimeError("Refusing an unexpected upload target.")
    else:
        url = BASE + path
    headers = {
        "Authorization": "Bearer " + TOKEN,
        "Accept": "application/vnd.github+json",
        "User-Agent": "VC5PvE-SharedAP-release",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    data = body
    if body is not None and not binary:
        headers["Content-Type"] = "application/json"
        data = json.dumps(body, ensure_ascii=False).encode("utf-8")
    elif binary:
        headers["Content-Type"] = "application/octet-stream"
    request = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=240) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        if not_found_ok and error.code == 404:
            return None
        raise SystemExit("GitHub API request failed with HTTP " + str(error.code) + "; response body suppressed.")
    except urllib.error.URLError:
        raise SystemExit("GitHub API request failed; network details suppressed.")


def summary(release):
    keys = ("id", "html_url", "tag_name", "target_commitish", "draft", "prerelease", "published_at")
    return {key: release.get(key) for key in keys} | {
        "assets": [
            {key: asset.get(key) for key in ("id", "name", "size", "state", "digest", "browser_download_url")}
            for asset in release.get("assets", [])
        ]
    }


def release_id():
    path = OUT / "github-release.json"
    if not path.is_file():
        raise SystemExit("Missing Builds/Release-0.3.0/github-release.json; run create first.")
    release = json.loads(path.read_text(encoding="utf-8"))
    if release.get("tag_name") != TAG or not isinstance(release.get("id"), int):
        raise SystemExit("Saved release metadata does not match this release tag.")
    return release["id"]


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return "sha256:" + digest.hexdigest()


def save_summary(release):
    value = summary(release)
    (OUT / "github-release.json").write_text(
        json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n"
    )
    print(json.dumps(value, ensure_ascii=False))


def create():
    commit = source_commit()
    refs = api("/git/ref/tags/" + urllib.parse.quote(TAG, safe=""), not_found_ok=True)
    releases = api("/releases?per_page=100")
    if refs is not None or any(item.get("tag_name") == TAG for item in releases):
        raise SystemExit("Tag or release already exists; refusing to overwrite it.")
    notes = OUT / "RELEASE-NOTES.md"
    if not notes.is_file():
        raise SystemExit("Missing Builds/Release-0.3.0/RELEASE-NOTES.md.")
    body = notes.read_text(encoding="utf-8-sig")
    release = api(
        "/releases",
        method="POST",
        body={
            "tag_name": TAG,
            "target_commitish": commit,
            "name": RELEASE_NAME,
            "body": body,
            "draft": True,
            "prerelease": True,
        },
    )
    if release.get("tag_name") != TAG or not release.get("draft"):
        raise SystemExit("GitHub did not create the expected draft release.")
    save_summary(release)


def upload(name):
    if name not in ASSETS:
        raise SystemExit("Asset name is not on the release allowlist.")
    path = OUT / name
    if not path.is_file():
        raise SystemExit("Release asset is missing: " + name)
    rid = release_id()
    release = api("/releases/" + str(rid))
    if release.get("tag_name") != TAG or not release.get("draft"):
        raise SystemExit("Uploads are allowed only to this task's draft release.")
    if any(asset.get("name") == name for asset in release.get("assets", [])):
        raise SystemExit("Asset already exists; refusing to replace it.")
    url = UPLOAD_BASE + str(rid) + "/assets?name=" + urllib.parse.quote(name, safe="")
    data = path.read_bytes()
    asset = api(url, method="POST", body=data, binary=True)
    expected_digest = "sha256:" + hashlib.sha256(data).hexdigest()
    if asset.get("size") != len(data) or asset.get("state") != "uploaded":
        raise SystemExit("Uploaded asset size or state does not match.")
    if asset.get("digest") and asset["digest"] != expected_digest:
        raise SystemExit("Uploaded asset digest does not match.")
    print(json.dumps({key: asset.get(key) for key in ("id", "name", "size", "state", "digest")}, ensure_ascii=False))


def status():
    release = api("/releases/" + str(release_id()))
    save_summary(release)


def publish():
    rid = release_id()
    release = api("/releases/" + str(rid))
    if release.get("tag_name") != TAG or not release.get("draft"):
        raise SystemExit("Only this task's draft release can be published.")
    existing = {asset.get("name"): asset for asset in release.get("assets", [])}
    for name in ASSETS:
        local = OUT / name
        remote = existing.get(name)
        if not local.is_file() or remote is None:
            raise SystemExit("A required release asset is missing: " + name)
        if remote.get("size") != local.stat().st_size or remote.get("state") != "uploaded":
            raise SystemExit("Uploaded asset size/state mismatch: " + name)
        if remote.get("digest") and remote["digest"] != sha256(local):
            raise SystemExit("Uploaded asset digest mismatch: " + name)
    published = api(
        "/releases/" + str(rid),
        method="PATCH",
        body={"draft": False, "prerelease": True, "make_latest": "false"},
    )
    if published.get("draft") or not published.get("prerelease"):
        raise SystemExit("GitHub did not publish as a prerelease.")
    save_summary(published)


def main():
    if len(sys.argv) < 2 or sys.argv[1] not in ("create", "upload", "status", "publish"):
        raise SystemExit("Usage: github-release.py create | upload <allowlisted-filename> | status | publish")
    mode = sys.argv[1]
    if mode == "create":
        create()
    elif mode == "upload":
        if len(sys.argv) != 3:
            raise SystemExit("Usage: github-release.py upload <allowlisted-filename>")
        upload(sys.argv[2])
    elif mode == "status":
        status()
    else:
        publish()


if __name__ == "__main__":
    main()
