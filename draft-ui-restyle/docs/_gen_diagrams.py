#!/usr/bin/env python3
"""Generates Draft's docs/02-erd.svg (from Data/Schema.sql) and
docs/03-uml.svg (from the real classes on the Draft branch).

Rules implemented: box width measured from the longest line (7px per char
at 11.5px, generous), 16px side padding, explicit fixed grid, >=90px
gutters wherever a label sits beside a line (>=40px elsewhere), straight
or single-elbow lines that never cross a box, labels beside lines not on
them, real crow's-feet + legend in the ERD, text >=10.5px stereo / 11.5px
body. Rendered to PNG afterwards for the visual check.
"""
import html

CHAR = 8.0   # conservative width for 11.5-13.5px text (DejaVu-measured)
PAD = 16
ROW = 18.5
HEAD = 30.0
STEREO_H = 14.0

CSS = """
      text { font-family: "Segoe UI", -apple-system, "Noto Sans", "DejaVu Sans", sans-serif; }
      .tbl   { fill:#FFFFFF; stroke:#06155A; stroke-width:2; }
      .hdr   { fill:#06155A; }
      .uihdr { fill:#5B21B6; }
      .hdrtxt{ fill:#FFFFFF; font-size:13.5px; font-weight:700; }
      .col   { font-size:11.5px; fill:#04060F; }
      .pk    { font-size:11.5px; fill:#8A5A00; font-weight:700; }
      .fk    { font-size:11.5px; fill:#1F6F43; font-weight:700; }
      .type  { font-size:10.5px; fill:#7A8794; }
      .rowline{ stroke:#E6E9F2; stroke-width:1; }
      .rel   { stroke:#0616C1; stroke-width:2; fill:none; }
      .reld  { stroke:#0616C1; stroke-width:2; fill:none; stroke-dasharray:7 4; }
      .lbl   { font-size:11.5px; fill:#55575B; }
      .note  { font-size:11.5px; fill:#55575B; }
      .notehd{ font-size:12.5px; fill:#06155A; font-weight:700; }
      .notebox{ fill:#F4F7FE; stroke:#B7D0F2; stroke-width:1.5; }
      .title { font-size:21px; font-weight:700; fill:#04060F; }
      .sub   { font-size:13px; fill:#7A8794; }
      .legend{ font-size:11.5px; fill:#04060F; }
      .cls   { fill:#FFFFFF; stroke:#06155A; stroke-width:2; }
      .ifc   { fill:#FFFFFF; stroke:#0616C1; stroke-width:2; stroke-dasharray:6 3; }
      .ui    { fill:#FFFFFF; stroke:#5B21B6; stroke-width:2; }
      .theme { fill:#FFFBEB; stroke:#B45309; stroke-width:2; }
      .stereo{ fill:#B7D0F2; font-size:10.5px; font-style:italic; }
      .m     { font-size:11.5px; fill:#04060F; }
      .sep   { stroke:#E6E9F2; stroke-width:1; }
"""


class Box:
    def __init__(self, x, y, title, rows, cls="tbl", hdr="hdr", stereo=None, min_w=0):
        self.x, self.y, self.title, self.rows = x, y, title, rows
        self.cls, self.hdr, self.stereo = cls, hdr, stereo
        lens = [len(title)] + [len(r if isinstance(r, str) else r[0]) for r in rows]
        if stereo:
            lens.append(int(len(stereo) * 6.3))
        self.w = max(min_w, int(max(lens) * CHAR) + PAD * 2)
        self.h = HEAD + (STEREO_H if stereo else 0) + len(rows) * ROW + 10

    def render(self):
        o = [f'<rect x="{self.x}" y="{self.y}" width="{self.w}" height="{self.h}" class="{self.cls}"/>',
             f'<rect x="{self.x}" y="{self.y}" width="{self.w}" height="{HEAD}" class="{self.hdr}"/>',
             f'<text x="{self.x + PAD}" y="{self.y + 20}" class="hdrtxt">{html.escape(self.title)}</text>']
        ty = self.y + HEAD
        if self.stereo:
            o.append(f'<text x="{self.x + PAD}" y="{ty + 10}" class="stereo">&#171;{html.escape(self.stereo)}&#187;</text>')
            ty += STEREO_H
        for i, row in enumerate(self.rows):
            text, css = row if isinstance(row, tuple) else (row, "m")
            ty += ROW
            o.append(f'<text x="{self.x + PAD}" y="{ty - 5}" class="{css}">{html.escape(text)}</text>')
            if i < len(self.rows) - 1:
                o.append(f'<line x1="{self.x + 1}" y1="{ty + 4}" x2="{self.x + self.w - 1}" y2="{ty + 4}" class="rowline"/>')
        return "\n".join(o)

    def right(self): return self.x + self.w
    def bottom(self): return self.y + self.h
    def midy(self): return self.y + self.h / 2


def arrow(parts, x1, y1, x2, y2, dashed=False, head="end"):
    """Straight segment with an arrowhead at 'end' or 'start'."""
    cls = "reld" if dashed else "rel"
    parts.append(f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" class="{cls}"/>')
    hx, hy = (x2, y2) if head == "end" else (x1, y1)
    import math
    dx, dy = x2 - x1, y2 - y1
    L = math.hypot(dx, dy) or 1
    ux, uy = dx / L, dy / L
    for s in (-1, 1):
        wx, wy = -uy * s, ux * s
        parts.append(f'<line x1="{hx}" y1="{hy}" x2="{hx - ux*11 + wx*5}" y2="{hy - uy*11 + wy*5}" class="{cls}"/>')


def elbow(parts, pts, dashed=False, head_at_last=True):
    cls = "reld" if dashed else "rel"
    d = "M " + " L ".join(f"{x} {y}" for x, y in pts)
    parts.append(f'<path class="{cls}" d="{d}"/>')
    if head_at_last:
        (x1, y1), (x2, y2) = pts[-2], pts[-1]
        import math
        dx, dy = x2 - x1, y2 - y1
        L = math.hypot(dx, dy) or 1
        ux, uy = dx / L, dy / L
        for s in (-1, 1):
            wx, wy = -uy * s, ux * s
            parts.append(f'<line x1="{x2}" y1="{y2}" x2="{x2 - ux*11 + wx*5}" y2="{y2 - uy*11 + wy*5}" class="{cls}"/>')


def vlabel(parts, x, y, text, anchor="start"):
    parts.append(f'<text x="{x}" y="{y}" text-anchor="{anchor}" class="lbl">{html.escape(text)}</text>')


def crowfoot(parts, x, y, towards, kind):
    d = -towards
    if kind == "one":
        for off in (8, 14):
            parts.append(f'<line x1="{x + d*off}" y1="{y-8}" x2="{x + d*off}" y2="{y+8}" class="rel"/>')
    elif kind == "zeromany":
        parts.append(f'<circle cx="{x + d*24}" cy="{y}" r="5" class="rel"/>')
        parts.append(f'<line x1="{x + d*14}" y1="{y}" x2="{x}" y2="{y-8}" class="rel"/>')
        parts.append(f'<line x1="{x + d*14}" y1="{y}" x2="{x}" y2="{y+8}" class="rel"/>')


def svg_doc(w, h, body):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}" width="{w}" height="{h}">\n'
            f'  <defs><style>{CSS}</style></defs>\n'
            f'  <rect width="{w}" height="{h}" fill="#FFFFFF"/>\n' + body + "\n</svg>\n")


# ================================================================== ERD
def kv(name, typ, note, key=None):
    text = f"{name:<26} {typ:<16} {note}"
    return (text, "pk" if key == "PK" else "fk" if key == "FK" else "col")


def build_erd():
    residents = Box(70, 130, "dbo.Residents", [
        kv("ResidentId", "int IDENTITY", "NOT NULL", "PK"),
        kv("Version", "int", "default 1"),
        kv("FirstName", "nvarchar(80)", "NOT NULL"),
        kv("MiddleName", "nvarchar(80)", "NOT NULL"),
        kv("LastName", "nvarchar(80)", "NOT NULL"),
        kv("Suffix", "nvarchar(20)", "NOT NULL"),
        kv("DateOfBirth", "date", "NOT NULL"),
        kv("Gender", "int", "CHECK 0-1"),
        kv("CivilStatus", "int", "CHECK 0-4"),
        kv("Purok", "nvarchar(60)", "NOT NULL"),
        kv("Address", "nvarchar(250)", "NOT NULL"),
        kv("ContactNumber", "nvarchar(15)", "NOT NULL"),
        kv("Occupation", "nvarchar(100)", "NOT NULL"),
        kv("DateOfResidency", "date", "CHECK >= DOB"),
        kv("IsRegisteredVoter", "bit", "NOT NULL"),
        kv("IsSeniorCitizen", "bit", "NOT NULL"),
        kv("IsPersonWithDisability", "bit", "NOT NULL"),
        kv("IsIndigent", "bit", "NOT NULL"),
        kv("IsStudent", "bit", "NOT NULL"),
        kv("IsSoloParent", "bit", "NOT NULL"),
        kv("HasUsedJobseekerBenefit", "bit", "NOT NULL"),
    ])
    requests = Box(700, 130, "dbo.DocumentRequests", [
        kv("RequestId", "int IDENTITY", "NOT NULL", "PK"),
        kv("Version", "int", "default 1"),
        kv("ResidentId", "int", "FK -> Residents", "FK"),
        kv("DocumentType", "int", "CHECK 0-6"),
        kv("DocumentName", "nvarchar(120)", "NOT NULL"),
        kv("Purpose", "nvarchar(300)", "NOT NULL"),
        kv("BusinessName", "nvarchar(120)", "NOT NULL"),
        kv("BusinessAddress", "nvarchar(250)", "NOT NULL"),
        kv("BusinessNature", "nvarchar(150)", "NOT NULL"),
        kv("DateRequested", "datetime2(7)", "NOT NULL"),
        kv("DateReleased", "datetime2(7)", "NULL"),
        kv("Status", "int", "CHECK 0-4"),
        kv("Fee", "decimal(12,2)", "CHECK >= 0"),
        kv("FeeBasis", "nvarchar(1000)", "NOT NULL"),
        kv("IsPaid", "bit", "NOT NULL"),
        kv("OfficialReceiptNumber", "nvarchar(50)", "NULL, case-insens."),
        kv("DatePaid", "datetime2(7)", "NULL"),
        kv("RejectionReason", "nvarchar(300)", "NOT NULL"),
        kv("ReleasedDocumentText", "nvarchar(max)", "NOT NULL"),
    ])
    snapshots = Box(1330, 130, "dbo.RequestResidentSnapshots", [
        kv("RequestId", "int", "PK, FK -> Requests", "PK"),
        kv("FirstName", "nvarchar(80)", "NOT NULL"),
        kv("MiddleName", "nvarchar(80)", "NOT NULL"),
        kv("LastName", "nvarchar(80)", "NOT NULL"),
        kv("Suffix", "nvarchar(20)", "NOT NULL"),
        kv("DateOfBirth", "date", "NOT NULL"),
        kv("Gender", "int", "NOT NULL"),
        kv("CivilStatus", "int", "NOT NULL"),
        kv("Purok", "nvarchar(60)", "NOT NULL"),
        kv("Address", "nvarchar(250)", "NOT NULL"),
        kv("ContactNumber", "nvarchar(15)", "NOT NULL"),
        kv("Occupation", "nvarchar(100)", "NOT NULL"),
        kv("DateOfResidency", "date", "NOT NULL"),
        kv("IsRegisteredVoter", "bit", "NOT NULL"),
        kv("IsSeniorCitizen", "bit", "NOT NULL"),
        kv("IsPersonWithDisability", "bit", "NOT NULL"),
        kv("IsIndigent", "bit", "NOT NULL"),
        kv("IsStudent", "bit", "NOT NULL"),
        kv("IsSoloParent", "bit", "NOT NULL"),
        kv("HasUsedJobseekerBenefit", "bit", "NOT NULL"),
    ])
    appstate = Box(70, 700, "dbo.AppState", [
        kv("Id", "int", "CHECK Id = 1", "PK"),
        kv("SchemaVersion", "int", "NOT NULL"),
        kv("SampleDataInitialized", "bit", "NOT NULL"),
    ])

    p = []
    ya = residents.y + HEAD + 60
    yb = requests.y + HEAD + 60
    xa, xb = residents.right(), requests.x
    p.append(f'<path class="rel" d="M {xa} {ya} L {xb} {yb}"/>')
    crowfoot(p, xa, ya, towards=1, kind="one")
    crowfoot(p, xb, yb, towards=-1, kind="zeromany")
    gx = (xa + xb) / 2
    for i, line in enumerate(["ResidentId - FK", "no ON DELETE cascade:",
                              "delete is blocked while", "request history exists"]):
        vlabel(p, gx, ya - 58 + i * 16, line, anchor="middle")

    ya2 = requests.y + requests.h - 100
    yb2 = snapshots.y + HEAD + 60
    xa2, xb2 = requests.right(), snapshots.x
    p.append(f'<path class="rel" d="M {xa2} {ya2} L {xa2 + 40} {ya2} L {xa2 + 40} {yb2} L {xb2} {yb2}"/>')
    crowfoot(p, xa2, ya2, towards=1, kind="one")
    crowfoot(p, xb2, yb2, towards=-1, kind="one")
    gx2 = xa2 + 40 + (xb2 - xa2 - 40) / 2
    for i, line in enumerate(["RequestId - PK + FK", "exactly one snapshot", "per request"]):
        vlabel(p, gx2, yb2 - 52 + i * 16, line, anchor="middle")

    for b in (residents, requests, snapshots, appstate):
        p.append(b.render())

    # notes panel under DocumentRequests
    notes = [
        ("Indexes on DocumentRequests", True),
        ("UX_Requests_Receipt    unique (OfficialReceiptNumber)", False),
        ("                           WHERE IsPaid = 1   (filtered)", False),
        ("UX_Requests_Jobseeker  unique (ResidentId)", False),
        ("                           WHERE DocumentType = 5", False),
        ("                             AND Status <> 4   (filtered)", False),
        ("IX_Requests_Resident   nonclustered (ResidentId)", False),
        ("", False),
        ("Rules the server enforces", True),
        ("CK_Requests_Payment     paid = receipt + date + fee > 0", False),
        ("CK_Requests_Release     released = date + text + paid-or-free", False),
        ("CK_Requests_Rejection   rejected = reason required", False),
        ("CK_Residents_Dates      residency on or after birth", False),
        ("FK_Requests_Residents   no cascade: delete blocked while", False),
        ("                           request history exists", False),
        ("", False),
        ("Snapshots", True),
        ("Request + snapshot inserts are one transaction.", False),
        ("Snapshot rows keep the resident details as they were", False),
        ("when the request was filed, on purpose.", False),
    ]
    nw = int(max(len(t) for t, _ in notes) * CHAR) + 28
    nx, ny = 700, 700
    nh = len(notes) * 17 + 16
    p.append(f'<rect x="{nx}" y="{ny}" width="{nw}" height="{nh}" class="notebox"/>')
    ty = ny + 6
    for text, head in notes:
        ty += 17
        if text:
            p.append(f'<text x="{nx + 14}" y="{ty - 5}" class="{"notehd" if head else "note"}">{html.escape(text)}</text>')

    # legend
    lg_x, lg_y = 70, 880
    p.append(f'<rect x="{lg_x}" y="{lg_y}" width="380" height="180" class="notebox"/>')
    p.append(f'<text x="{lg_x+14}" y="{lg_y+24}" class="notehd">Legend</text>')
    ly = lg_y + 48
    p.append(f'<line x1="{lg_x+14}" y1="{ly}" x2="{lg_x+72}" y2="{ly}" class="rel"/>')
    crowfoot(p, lg_x + 72, ly, towards=1, kind="one")
    p.append(f'<text x="{lg_x+86}" y="{ly+4}" class="legend">exactly one</text>')
    ly += 32
    p.append(f'<line x1="{lg_x+14}" y1="{ly}" x2="{lg_x+72}" y2="{ly}" class="rel"/>')
    crowfoot(p, lg_x + 72, ly, towards=1, kind="zeromany")
    p.append(f'<text x="{lg_x+86}" y="{ly+4}" class="legend">zero or many</text>')
    ly += 32
    p.append(f'<rect x="{lg_x+14}" y="{ly-12}" width="58" height="18" class="tbl"/>')
    p.append(f'<text x="{lg_x+86}" y="{ly+2}" class="legend">table - every column from Data/Schema.sql</text>')
    ly += 30
    p.append(f'<text x="{lg_x+14}" y="{ly}" class="pk">PK</text>')
    p.append(f'<text x="{lg_x+44}" y="{ly}" class="legend">primary key</text>')
    p.append(f'<text x="{lg_x+160}" y="{ly}" class="fk">FK</text>')
    p.append(f'<text x="{lg_x+190}" y="{ly}" class="legend">foreign key</text>')
    ly += 26
    p.append(f'<text x="{lg_x+14}" y="{ly}" class="type">CHECK</text>')
    p.append(f'<text x="{lg_x+62}" y="{ly}" class="legend">constraint or default kept from the SQL</text>')

    header = ('<text x="36" y="42" class="title">Entity Relationship Diagram &#8212; Barangay Document System (Draft)</text>'
              '<text x="36" y="64" class="sub">SQL Server Express LocalDB &#183; database BarangayDocumentSystem &#183; every table, column, type, key, index and rule from Data/Schema.sql</text>')
    return svg_doc(1900, 1120, header + "\n" + "\n".join(p))


# ================================================================== UML
def build_uml():
    p = []

    def box_at(x, y, title, rows, cls="cls", hdr="hdr", stereo=None):
        b = Box(x, y, title, rows, cls=cls, hdr=hdr, stereo=stereo)
        p.append(b.render())
        return b

    # ---- band 1: UI (y=130) -----------------------------------------
    mainf = box_at(60, 130, "MainForm", [
        "+ navigation sidebar shell",
        "+ switches the three pages",
        "+ hosts the page controls",
    ], cls="ui", hdr="uihdr", stereo="form")
    ctrls = box_at(420, 130, "The three page controls", [
        "DashboardControl",
        "ResidentsControl",
        "RequestsControl",
        "+ designer files hold the layout,",
        "+ .cs files hold the behaviour,",
        "+ RefreshData() after every change",
    ], cls="ui", hdr="uihdr", stereo="user controls")
    forms = box_at(880, 130, "The dialog forms", [
        "ResidentForm, RequestForm,",
        "PaymentForm, RejectionForm,",
        "DocumentPreviewForm",
        "+ typed dialogs; the designers",
        "open untouched",
    ], cls="ui", hdr="uihdr", stereo="forms")
    printjob = box_at(1300, 130, "DocumentPrintJob", [
        "+ PrintDocument pipeline",
        "+ TextPaginator: fit the text,",
        "continue on the next page",
    ], cls="ui", hdr="uihdr", stereo="printing")

    # ---- band 2: theme + helpers (y=350 / 330) -----------------------
    theme = box_at(420, 350, "ModernTheme, ModernControls", [
        "+ navy / gold palette tokens",
        "+ Inter from Assets\\fonts,",
        "Segoe UI fallback, cached",
        "+ rounded cards, shadows,",
        "pills, hero banner, bars",
        "+ ApplyTheme() after",
        "InitializeComponent(),",
        "guarded for the designer",
    ], cls="theme", hdr="uihdr", stereo="theme, new")
    helpers = box_at(1300, 330, "UiFeedback, ErrorLogger", [
        "AppSettings reads App.config",
        "+ expected errors shown,",
        "+ unexpected logged to file",
    ], cls="ui", hdr="uihdr", stereo="helpers")

    # ---- band 3: services (y=620) ------------------------------------
    rsvc = box_at(60, 620, "ResidentService", [
        "+ Search(term)",
        "+ Save(details) / Delete",
    ], stereo="service")
    val = box_at(380, 620, "ResidentValidator", [
        "+ required fields and dates",
        "+ contact number format",
        "+ classification flags",
    ], stereo="service")
    qsvc = box_at(680, 620, "RequestService", [
        "+ File(details) - prices the fee",
        "+ StartProcessing, MarkReady",
        "+ Release, Reject(reason)",
        "+ RecordPayment(receipt)",
        "+ jobseeker eligibility check",
    ], stereo="service")
    fee = box_at(1040, 620, "FeeSchedule", [
        "+ Assess(): FeeAssessment",
        "+ PHP 100 residency / good moral",
        "+ PHP 50 clearance, PHP 200",
        "business clearance",
        "+ senior / PWD / indigent waiver",
    ], stereo="service")
    rend = box_at(1400, 620, "DocumentRenderer", [
        "+ letterhead + footer",
        "+ Render(request): string",
    ], stereo="service")
    rept = box_at(1720, 620, "ReportingService", [
        "+ GetStatistics():",
        "BarangayStatistics",
    ], stereo="service")

    # ---- band 4: interfaces + data (y=854) ----------------------------
    ibar = Box(60, 854, "IBarangayRepository", [
        "+ GetResidents() / GetRequests()",
        "+ SaveResident(resident)",
        "+ SaveRequest(request)",
        "+ DeleteResident(resident)",
        "+ ReceiptNumberExists(receipt)",
        "+ RequestCountForResident(id)",
    ], cls="ifc", stereo="interface")
    p.append(ibar.render())
    sqlrepo = box_at(430, 854, "SqlBarangayRepository", [
        "+ parameterized SQL",
        "+ optimistic Version check",
        "+ maps rows to models",
    ], stereo="class")
    sqldb = box_at(790, 854, "SqlDatabase", [
        "+ creates the schema on first run",
        "+ seeds samples once, in a",
        "transaction; version check",
    ], stereo="class")
    inmem = box_at(1150, 854, "InMemoryBarangayRepository", [
        "+ lists instead of tables",
        "+ same rules; used by tests",
    ], stereo="implements IBarangayRepository")
    idoc = Box(1500, 854, "IDocumentTemplate", [
        "+ DocumentType, Title",
        "+ BuildBody(resident, request,",
        "profile): string",
    ], cls="ifc", stereo="interface")
    p.append(idoc.render())
    tmpl = box_at(1830, 854, "7 document templates", [
        "Clearance, Residency, Indigency,",
        "Jobseeker, BusinessClearance,",
        "GoodMoral, BarangayId",
    ], stereo="realize IDocumentTemplate")

    # ---- band 5: models (y=1108) --------------------------------------
    resident = box_at(60, 1108, "Resident", [
        "+ ResidentId, name fields, suffix",
        "+ dates, gender, civil status",
        "+ purok, address, contact, job",
        "+ voter + classification flags",
        "+ jobseeker benefit flag",
        "+ GetFullName / GetAge / Sortable",
    ], stereo="entity")
    docreq = box_at(470, 1108, "DocumentRequest", [
        "+ RequestId, Resident, type",
        "+ DocumentName, purpose, business",
        "+ dates requested / released",
        "+ Status, Fee, FeeBasis, IsPaid",
        "+ receipt number, DatePaid",
        "+ RejectionReason",
        "+ ReleasedDocumentText",
        "+ Start / MarkReady / Release /",
        "Reject / RecordPayment",
    ], stereo="entity")
    records = box_at(900, 1108, "Records and enums", [
        "RequestDetails, FeeAssessment,",
        "BarangayStatistics, BarangayProfile",
        "enum DocumentType (7 values)",
        "enum RequestStatus (5 values)",
        "enum Gender, enum CivilStatus",
    ], stereo="models")

    # ---- edges: straight or single-elbow, in the gutters only ---------
    # band 1 horizontals
    y = mainf.bottom() - 26
    arrow(p, mainf.right(), y, ctrls.x, y)
    vlabel(p, (mainf.right() + ctrls.x) / 2, y - 10, "hosts", anchor="middle")
    arrow(p, ctrls.right(), y, forms.x, y)
    vlabel(p, (ctrls.right() + forms.x) / 2, y - 10, "opens", anchor="middle")
    y2 = forms.bottom() - 60
    arrow(p, forms.right(), y2, printjob.x, y2)
    vlabel(p, (forms.right() + printjob.x) / 2, y2 - 10, "prints through", anchor="middle")

    # page controls down to the theme box (same column)
    x = 500
    arrow(p, x, ctrls.bottom(), x, theme.y)
    vlabel(p, x + 10, (ctrls.bottom() + theme.y) / 2 + 4, "ApplyTheme()")

    # controls -> ReportingService: leave from the right edge, run the
    # channel between the theme box and the helpers box, then across
    elbow(p, [(ctrls.right(), ctrls.bottom() - 40), (780, ctrls.bottom() - 40),
              (780, 580), (1810, 580), (1810, rept.y)])
    vlabel(p, 800, 566, "GetStatistics() feeds the dashboard")

    # band 3 horizontals
    y = 700
    arrow(p, val.x, y, rsvc.right(), y, head="start")
    vlabel(p, (val.x + rsvc.right()) / 2, y + 20, "validates", anchor="middle")
    arrow(p, fee.x, y, qsvc.right(), y, head="start")
    vlabel(p, (fee.x + qsvc.right()) / 2, y + 20, "prices", anchor="middle")

    # RequestService -> DocumentRenderer (channel under band 3)
    elbow(p, [(820, qsvc.bottom()), (820, 820), (1490, 820), (1490, rend.bottom() + 2)],
          head_at_last=True)
    vlabel(p, 900, 812, "builds the released text with")

    # services down to the repository interface
    x = 160
    arrow(p, x, rsvc.bottom(), x, ibar.y)
    vlabel(p, x + 10, rsvc.bottom() + 34, "uses")
    elbow(p, [(720, qsvc.bottom()), (720, 800), (240, 800), (240, ibar.y)])
    vlabel(p, 260, 792, "reads and writes through")

    # renderer -> template interface
    x = 1570
    arrow(p, x, rend.bottom(), x, idoc.y)
    vlabel(p, x + 10, rend.bottom() + 34, "renders via")

    # realizations
    y = 950
    arrow(p, sqlrepo.x, y, ibar.right(), y, dashed=True, head="start")
    vlabel(p, (sqlrepo.x + ibar.right()) / 2, y + 22, "implements", anchor="middle")
    arrow(p, tmpl.x, y, idoc.right(), y, dashed=True, head="start")
    vlabel(p, (tmpl.x + idoc.right()) / 2, y + 22, "realizes", anchor="middle")
    arrow(p, sqlrepo.right(), y, sqldb.x, y, head="end")
    vlabel(p, (sqldb.x + sqlrepo.right()) / 2, y + 22, "schema", anchor="middle")

    # interface -> models
    x = 190
    arrow(p, x, ibar.bottom(), x, resident.y)
    vlabel(p, x + 10, ibar.bottom() + 34, "returns")
    elbow(p, [(300, ibar.bottom()), (300, 1062), (580, 1062), (580, docreq.y)])
    vlabel(p, 600, 1072, "returns")

    # ---- legend --------------------------------------------------------
    lg_y = 1440
    p.append(f'<rect x="60" y="{lg_y}" width="820" height="140" class="notebox"/>')
    p.append(f'<text x="74" y="{lg_y+24}" class="notehd">Legend</text>')
    ly = lg_y + 48
    arrow(p, 74, ly, 150, ly)
    p.append(f'<text x="164" y="{ly+4}" class="legend">calls / uses / hosts</text>')
    arrow(p, 360, ly, 436, ly, dashed=True)
    p.append(f'<text x="450" y="{ly+4}" class="legend">implements / realizes</text>')
    ly += 46
    p.append(f'<rect x="74" y="{ly-14}" width="74" height="20" class="cls"/>')
    p.append(f'<text x="164" y="{ly}" class="legend">class (all real, from the branch)</text>')
    p.append(f'<rect x="424" y="{ly-14}" width="74" height="20" class="ifc"/>')
    p.append(f'<text x="514" y="{ly}" class="legend">interface</text>')
    p.append(f'<rect x="620" y="{ly-14}" width="74" height="20" class="theme"/>')
    p.append(f'<text x="710" y="{ly}" class="legend">theme files this restyle adds</text>')

    header = ('<text x="36" y="42" class="title">UML Class Diagram &#8212; Barangay Document System (Draft)</text>'
              '<text x="36" y="64" class="sub">.NET Framework 4.7.2 &#183; WinForms &#183; only classes that exist on the branch &#183; amber boxes are the theme files this restyle adds</text>')
    return svg_doc(2140, 1620, header + "\n" + "\n".join(p))


import pathlib
here = pathlib.Path(__file__).parent
(here / "02-erd.svg").write_text(build_erd(), encoding="utf-8")
(here / "03-uml.svg").write_text(build_uml(), encoding="utf-8")
print("written")
