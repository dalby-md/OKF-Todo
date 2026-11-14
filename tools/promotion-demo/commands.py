import json
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET
import zipfile

from paths import DB, KIT, REPO, RUN

APPLICATION = REPO / "Okf-Todo" / "bin" / "Release" / "net8.0" / "Okf-Todo.dll"


def call(kind, payload, label):
    if not APPLICATION.is_file():
        raise SystemExit("Build Okf-Todo/Okf-Todo.csproj in Release first.")
    if DB.resolve() != DB or DB.parent.resolve() != DB.parent:
        raise RuntimeError("The demo database path must stay inside the checkout artifacts.")
    DB.parent.mkdir(parents=True, exist_ok=True)
    request = dict(messageId=uuid.uuid4().hex, type=kind, payload=payload)
    result = subprocess.run(
        ["dotnet", str(APPLICATION), "--okf-command", "--okf-database-path", str(DB)],
        input=json.dumps(request), text=True, encoding="utf-8",
        capture_output=True, timeout=60,
    )
    (RUN / f"{label}.stderr.log").write_text(result.stderr, encoding="utf-8")
    (RUN / f"{label}.json").write_text(result.stdout, encoding="utf-8")
    result.check_returncode()
    response = json.loads(result.stdout)
    if not response["ok"]:
        raise RuntimeError(f"{kind}: {response}")
    return response["payload"]


def read_workbook():
    ns = {"x": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
    with zipfile.ZipFile(KIT / "work-notes.xlsx") as workbook:
        shared = []
        if "xl/sharedStrings.xml" in workbook.namelist():
            shared = ["".join(item.itertext())
                      for item in ET.fromstring(workbook.read("xl/sharedStrings.xml"))]
        sheet = ET.fromstring(workbook.read("xl/worksheets/sheet1.xml"))
    rows = []
    for row in sheet.findall("x:sheetData/x:row", ns):
        values = []
        for cell in row.findall("x:c", ns):
            value = cell.findtext("x:v", namespaces=ns)
            if value is None:
                value = "".join(cell.itertext())
            values.append(shared[int(value)] if cell.get("t") == "s" else value)
        if values and values[0].startswith("SRC-"):
            rows.append(values)
    return rows


def discover():
    if DB.exists():
        raise SystemExit(f"Demo database already exists. Rename {RUN} before starting over.")
    rows = read_workbook()
    if len(rows) != 6 or any(len(row) != 4 for row in rows):
        raise RuntimeError("Expected six workbook rows with four columns each.")
    lists = call("taskList.list", {}, "01-lists")
    lookups = call("task.lookups.get", {}, "02-lookups")
    if call("task.list", {"view": "all"}, "03-empty-tasks"):
        raise RuntimeError("Expected an empty demo database.")
    (RUN / "source-notes.json").write_text(json.dumps(rows, indent=2), encoding="utf-8")
    print(json.dumps(dict(database=str(DB), sourceRows=rows, lists=lists, lookups=lookups), indent=2))


if __name__ == "__main__":
    if sys.argv[1:] != ["discover"]:
        raise SystemExit("Usage: python commands.py discover")
    discover()
