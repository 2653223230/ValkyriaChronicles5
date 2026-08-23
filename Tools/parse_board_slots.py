import re
from pathlib import Path

text = Path(r"Assets/TcgEngine/Scenes/Game/Game.unity").read_text(encoding="utf-8", errors="ignore")
blocks = re.split(r"--- !u!1001 &\d+\n", text)
slots = []
for b in blocks:
    if "BoardSlot" not in b or "propertyPath: x" not in b:
        continue
    name = re.search(r"value: (BoardSlot[^\n]*)", b)
    x = re.search(r"propertyPath: x\n\s+value: (\d+)", b)
    y = re.search(r"propertyPath: y\n\s+value: (\d+)", b)
    lx = re.search(r"propertyPath: m_LocalPosition\.x\n\s+value: ([^\n]+)", b)
    ly = re.search(r"propertyPath: m_LocalPosition\.y\n\s+value: ([^\n]+)", b)
    if x and y and lx and ly:
        slots.append(
            {
                "x": int(x.group(1)),
                "y": int(y.group(1)),
                "lx": float(lx.group(1)),
                "ly": float(ly.group(1)),
                "name": name.group(1) if name else "",
            }
        )

slots.sort(key=lambda s: (-s["ly"], s["lx"]))
print("count", len(slots))
for s in sorted(slots, key=lambda z: (z["y"], z["x"])):
    print(f"({s['x']},{s['y']}) world=({s['lx']},{s['ly']})")