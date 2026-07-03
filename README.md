# SalohiyatTable — AutoCAD nuqta-koordinata jadvali plagini

AutoCAD chizmasida belgilangan nuqtalarni **raqamlab**, ular yoniga koordinata va
masofa **jadvalini** avtomatik yasovchi plagin. Jadval ostiga **yer maydoni**
(m.kv va gektar) hamda **chegara uzunligi** (metr) yoziladi.

Plagin ikki qismdan iborat:
- **`SalohiyatTable.dll`** — asosiy mantiq (AutoCAD .NET / ObjectARX API, C#).
- **`LoadPtable.lsp`** — DLL ni `NETLOAD` orqali yuklovchi AutoLISP fayli.

---

## Jadval ko'rinishi

| Nuqtalar № | Geo ma'lumotlar |||
|:---:|:---:|:---:|:---:|
|      | **Uzunligi(m)** | **X** | **Y** |
| 1 |     | X1 | Y1 |
|   | d(1→2) |    |    |
| 2 |     | X2 | Y2 |
|   | d(2→3) |    |    |
| … |     | …  | …  |
| 1 |     | X1 | Y1 |

Jadval ostida:

```
Yer maydoni: <yuza> m.kv (<gektar> ga)
Chegara uzunligi: <perimetr> m
```

- Har bir **nuqta qatori** raqam, X va Y koordinatalarni ko'rsatadi.
- Har bir **masofa qatori** shu nuqtadan keyingi nuqtagacha bo'lgan masofani (metr) ko'rsatadi.
- Yopiq kontur uchun oxirida **1-nuqta** yana takrorlanib, kontur yopiladi.
- Koordinatalar chizma birligida (odatda **metr**) olinadi; `1 birlik = 1 metr` deb qabul qilinadi.

---

## Buyruqlar

| Buyruq | Vazifasi |
|--------|----------|
| `PTABLE`    | Nuqtalarni sichqoncha bilan ketma-ket ko'rsatib koordinata jadvali yasash. |
| `PLTABLE`   | Mavjud **poliliniya** (LWPOLYLINE) cho'qqilaridan koordinata jadvali yasash. |
| `PTCHEGARA` | Nuqtalardan **chegaradoshlar jadvali** (faqat raqamlar). |
| `PLCHEGARA` | Poliliniyadan **chegaradoshlar jadvali** (faqat raqamlar). |
| `PTPOLIGON` | GPS nuqta bloklaridan tartib raqamlari bo'yicha **poligon** (yopiq poliliniya) yasash. |
| `PTTOMORQA` | Yopiq maydondan ichkariga surilgan **Tomorqa** poligoni (nuqtali) + markazga "Tomorqa" yozuvi. |
| `PTSOZLAMA` | Sozlamalar oynasi (matn balandligi, o'nlik xonalar, nuqta belgisi, jadval burchagi). |
| `PTHAQIDA`  | Plagin va mualliflar haqida ("Haqida" oynasi). |
| `PTLOAD`    | DLL ni qayta yuklash (LISP funksiyasi). |

`PTABLE` / `PLTABLE` da faqat **jadval joyi** (yuqori-chap burchak) so'raladi.
Matn balandligi, o'nlik xonalar soni, nuqta belgisi turi va **jadval burchagi**
(ko'rsatilgan nuqta jadvalning qaysi burchagi bo'lishi: chap-yuqori / o'ng-yuqori /
chap-pastki / o'ng-pastki) **sozlamalardan** olinadi (`PTSOZLAMA` orqali o'zgartiriladi
va **saqlanadi** — keyingi seanslarda ham qoladi).

- Tartib raqamlari har doim **poligondan tashqariga** joylashtiriladi (kontur ichida qolmaydi).
- Ustun kengliklari katakdagi eng uzun matnga qarab **avtomatik moslashadi** (jadval eniga ham moslashadi).
- Sozlamalar `%APPDATA%\SalohiyatTable\settings.xml` faylida saqlanadi.

---

## Lentadagi menyu (Ribbon)

Plagin yuklangach, AutoCAD lentasida (ribbon) **"SalohiyatTable"** yorlig'i paydo bo'ladi:
- **"Jadval"** paneli: **Nuqtalardan jadval** (`PTABLE`), **Poliliniyadan jadval** (`PLTABLE`).
- **"Sozlamalar"** paneli: **Sozlamalar** (`PTSOZLAMA`), **Haqida** (`PTHAQIDA`).

- **"Chegaradoshlar"** paneli: **Chegaradoshlar (nuqtalardan)** (`PTCHEGARA`), **Chegaradoshlar (poliliniya)** (`PLCHEGARA`).
- **"Poligon"** paneli: **Nuqtalardan poligon yaratish** (`PTPOLIGON`), **Tomorqa yaratish** (`PTTOMORQA`).

**Mualliflar** ma'lumoti jadvalda emas — menyudagi **"Haqida"** oynasida ko'rsatiladi.

### Chegaradoshlar jadvali

Jadval ustida **Ijrochi** (sozlamalardan) va **Buyurtmachi** satrlari (tag chiziq bilan)
chiziladi. Jadvalning o'ziga **faqat raqamlar** qo'yiladi (koordinatasiz); "Chegaradoshlar"
ustuni qo'lda to'ldirish uchun bo'sh qoladi.

```
Ijrochi: <sozlamalardan>            __________________________
Buyurtmachi:                        __________________________
┌───────────────────────────────────────────────────────────┐
│      Yer uchastkasining chegara burulish nuqtalari tasnifi  │
├───────────────────┬─────────────────────────────────────────┤
│ Burulish nuqtalari│                                         │
├────────┬──────────┤            Chegaradoshlar               │
│  dan   │  gacha   │                                         │
├────────┼──────────┼─────────────────────────────────────────┤
│   1    │    2     │                                         │
│   2    │    3     │                                         │
│  ...   │   ...    │                                         │
└────────┴──────────┴─────────────────────────────────────────┘
```

**Sozlamalarda** (`PTSOZLAMA`) chegaradoshlar jadvali uchun quyidagilar bor:
- **Ijrochi** — ismi (jadval ustida ko'rsatiladi, saqlanadi).
- **Chegaradoshlar burchagi** — bu jadval qaysi burchagidan qo'yilishi (asosiy jadvaldan mustaqil).
- **Chegaradosh matn balandligi** — chegaradoshlar jadvali uchun alohida matn balandligi (asosiy jadvaldan mustaqil).
- **Chegaradosh ustuni (×h)** — "Chegaradoshlar" ustuni kengligi (matn balandligiga nisbatan koeffitsient).

**Buyurtmachi** satri va har bir "Chegaradoshlar" katagi tag chiziq (`___`) bilan chiziladi —
matnni ustiga bosib (double-click) qo'lda yozish oson bo'lishi uchun.
Sarlavha qatorlari balandligi ham matnga (o'ralgan satr soniga) moslashadi.

---

## Nuqtalardan poligon yasash (`PTPOLIGON`)

GPS qurilmasidan olingan nuqtalar chizmada odatda **`POINT` (Точка) obyekti** bo'lib, yonida
**tartib raqami** yozilgan matn (DBText/MText) turadi (raqam, tavsif, balandlik). `PTPOLIGON`
buyrug'i kiritilgan raqamli nuqtalarni **ketma-ket tutashtirib yopiq poliliniya (poligon)**
yasaydi. (Nuqtalar atributli blok bo'lsa — butun sonli atribut raqam sifatida olinadi.)

Ishlatish:
1. `PTPOLIGON` buyrug'ini yozing.
2. Raqamlarni kiriting, masalan: `145-165, 171, 182-260`
   - `a-b` — diapazon (`145-165` = 145, 146, …, 165).
   - Vergul bilan ajratiladi; alohida raqam ham bo'ladi (`171`).
3. Shu raqamli nuqtalar (blok insert nuqtalari) berilgan tartibda tutashtirilib poligon chiziladi.
4. Topilmagan raqamlar (agar bo'lsa) buyruq qatorida ko'rsatiladi.

> Raqamli matn (sof butun son, masalan `757`) o'ziga **eng yaqin `POINT`** ga bog'lanadi;
> o'nlik balandlik (`874.77`) yoki matnli tavsif (`Kõca bowi`) raqam deb olinmaydi.
> Yasalgan poligonni keyin `PLTABLE` yoki `PLCHEGARA` bilan jadvalga aylantirish mumkin.

---

## Tomorqa yeri yasash (`PTTOMORQA`)

Yopiq (atrofi o'ralgan) maydonning tashqi chegarasidan **ichkariga** ma'lum masofaga
surilgan poligon chizadi va markaziga **"Tomorqa"** deb yozadi.

Ishlatish:
1. `PTTOMORQA` buyrug'ini yozing.
2. Tomorqa maydoni **ichidan** bitta nuqta ko'rsating.
3. Dastur chegarani avtomatik aniqlaydi (BOUNDARY kabi), undan ichkariga surilgan
   **nuqtali (DOT)** poligon chizadi va o'rtasiga "Tomorqa" yozadi.

**Sozlamalar** (`PTSOZLAMA`):
- **Tomorqa ichkariga (m)** — tashqi chegaradan ichkariga masofa (standart `2`).
- **Tomorqa matn balandligi** — "Tomorqa" yozuvi balandligi (standart `2.5`).
- **Tomorqa burchaklari** — `Qirrali` (o'tkir) yoki `Yoysimon` (yumaloqlangan).
- **Tomorqa yoy radiusi** — `Yoysimon` tanlanganda burchak yoyining radiusi (standart `2`).
- **Tomorqa chiziq masshtabi** — nuqtali chiziq masshtabi/LTSCALE (standart `1`; nuqtalar
  ko'rinmasa yoki juda zich bo'lsa shu qiymatni o'zgartiring).

> Nuqta ko'rsatilgan joy atrofi to'liq **yopiq** bo'lishi kerak (chiziqlar tutashgan).
> Masofa maydonga nisbatan juda katta bo'lsa, ichki poligon yasalmaydi (ogohlantiradi).

> **Muhim:** menyu tugmalari `AdWindows.dll` sborkasiga bog'liq, u esa NuGet'da yo'q —
> faqat AutoCAD o'rnatilgan papkada bo'ladi. Shuning uchun menyu paydo bo'lishi uchun
> `src/SalohiyatTable.csproj` dagi **`AutoCADPath`** ni o'z AutoCAD papkangizga
> to'g'rilang (masalan `C:\Program Files\Autodesk\AutoCAD 2022`) va qayta build qiling.
>
> Agar `AutoCADPath` ko'rsatilmasa yoki noto'g'ri bo'lsa, build baribir muvaffaqiyatli
> bo'ladi, lekin menyu bo'lmaydi — bunda `PTABLE` / `PLTABLE` buyruqlarini buyruq
> qatoriga yozib ishlatasiz.
>
> Menyu ko'rinmasa: lentaning o'zi yoqilganini tekshiring (`RIBBON` buyrug'i), va
> DLL `NETLOAD` qilinganini tasdiqlang.

---

## Kompilyatsiya (Build)

Talablar:
- Windows + o'rnatilgan **AutoCAD** (2019+ tavsiya etiladi).
- **.NET Framework 4.8** SDK va **Visual Studio 2019/2022** yoki `dotnet` / `msbuild`.

Loyiha AutoCAD .NET API (`acmgd`, `acdbmgd`, `accoremgd`) ga bog'lanadi. Bu sborkalar
**NuGet** paketi `AutoCAD.NET` orqali avtomatik olinadi — AutoCAD o'rnatilgan papkani
qo'lda ko'rsatish shart emas.

### 1-qadam: AutoCAD versiyangizni tanlang

`src/SalohiyatTable.csproj` faylidagi `AcadNetVersion` ni o'z AutoCAD versiyangizga
(yoki undan **pastroq**) moslang:

| AutoCAD | `AcadNetVersion` |
|---------|------------------|
| 2019 | `23.0.0` |
| 2020 | `23.1.0` |
| 2021 | `24.0.0` |
| 2022 | `24.1.0` |
| 2023 | `24.2.0` |
| 2024 | `24.3.0` |
| 2025 | `25.0.0` (bunda `TargetFramework` ni `net8.0-windows` qiling) |

> Past versiyaga qurilgan plagin yuqori AutoCAD'da ham ishlaydi, teskarisi emas.
> Standart qiymat: `23.1.0` (AutoCAD 2020), 2020+ larda ishlaydi.

### 2-qadam: Build

**Visual Studio orqali (tavsiya etiladi):**
1. `SalohiyatTable.sln` faylini Visual Studio'da oching.
2. Internet ulanishi bo'lsin — NuGet paketlarini avtomatik tiklaydi (Restore).
3. Konfiguratsiya = **Release**, platforma = **x64**.
4. **Build → Build Solution** (yoki `Ctrl+Shift+B`).

**Buyruq qatoridan (msbuild):**
```powershell
msbuild SalohiyatTable.sln /t:Restore /p:Configuration=Release /p:Platform=x64
msbuild SalohiyatTable.sln /p:Configuration=Release /p:Platform=x64

# yoki bitta buyruqda restore bilan:
dotnet build SalohiyatTable.sln -c Release
```

Natijada `SalohiyatTable.dll` hosil bo'ladi (`src\bin\Release\`).

> **Lentadagi menyu uchun:** `csproj` dagi `AutoCADPath` to'g'ri ko'rsatilsa,
> build vaqtida `AdWindows.dll` topiladi va menyu qo'shiladi. Aks holda faqat
> buyruqlar (PTABLE/PLTABLE) ishlaydi — build baribir muvaffaqiyatli bo'ladi.

> **Eslatma:** `net48` ko'pchilik AutoCAD versiyalari (2019–2024) uchun mos.
> AutoCAD 2025+ uchun `TargetFramework` ni `net8.0-windows` ga o'zgartiring.

---

## O'rnatish (eng oddiy va ishonchli usul — Startup Suite)

Bu usulda DLL yo'li `.lsp` faylga **bir marta** yoziladi va fayl AutoCAD'ning
**Startup Suite** iga bir marta qo'shiladi. Keyin AutoCAD har ochilganda plagin
avtomatik yuklanadi.

1. `SalohiyatTable.dll` va `LoadPtable.lsp` ni doimiy bir papkaga joylang
   (masalan `C:\Plugins\SalohiyatTable\`).
2. `LoadPtable.lsp` ni bloknotda oching va yuqoridagi qatorni o'z yo'lingizga moslang:
   ```lisp
   (setq SALOHIYAT_DLL_PATH "C:\\SalohiyatTable.dll")
   ```
   > Yo'lda `\` o'rniga `\\` yoki `/` ishlating (masalan `C:/Plugins/SalohiyatTable/SalohiyatTable.dll`).
3. AutoCAD'da `APPLOAD` buyrug'ini yozing → **Startup Suite** (Приложения) → **Add...** (Добавить)
   → `LoadPtable.lsp` ni tanlang → **Close**.
4. AutoCAD'ni qayta ishga tushiring.
5. Buyruqlar tayyor: `PTABLE`, `PLTABLE`, `PTCHEGARA`, `PLCHEGARA`, `PTSOZLAMA`, `PTHAQIDA`.

> Fayl AutoCAD versiyasi almashsa ham ishlaydi — faqat kerak bo'lsa DLL ni mos
> versiyaga qayta build qiling. `.lsp` ni Startup Suite ga qayta qo'shish shart emas.

**Muqobil yo'l:** har safar `APPLOAD` orqali `SalohiyatTable.dll` ni qo'lda `NETLOAD` qilish.

---

## Loyiha tuzilishi

```
AutoCAD_table/
├── SalohiyatTable.sln          # Visual Studio solution (Release|x64)
├── src/
│   ├── SalohiyatTable.csproj   # .NET loyiha fayli
│   ├── Commands.cs             # PTABLE / PLTABLE / PTSOZLAMA / PTHAQIDA buyruqlari
│   ├── RibbonUi.cs             # lentadagi menyu (Ribbon tab + tugmalar)
│   ├── PluginSettings.cs       # saqlanadigan sozlamalar (XML) + plagin ma'lumoti
│   ├── SettingsForm.cs         # sozlamalar oynasi (WinForms)
│   ├── AboutForm.cs            # "Haqida" oynasi (mualliflar)
│   ├── PointCollector.cs       # nuqta yig'ish (interaktiv / poliliniya)
│   ├── MarkerDrawer.cs         # nuqta raqamlarini chizish
│   ├── TableBuilder.cs         # koordinata jadvali + yuza/perimetr matni
│   ├── NeighborsTableBuilder.cs # chegaradoshlar jadvali (faqat raqamlar)
│   ├── GeometryHelper.cs       # masofa, perimetr, yuza (Shoelace)
│   └── TableOptions.cs         # o'lcham/format sozlamalari
├── lisp/
│   └── LoadPtable.lsp          # NETLOAD yuklovchisi
├── .gitignore
└── README.md
```

---

## Sozlash

Sonlar formati va o'lchamlarni `src/TableOptions.cs` faylidan o'zgartirish mumkin:
- `CoordFormat` — X/Y koordinatalar aniqligi (standart `0.###`).
- `LenFormat` — masofa/perimetr aniqligi (standart `0.##`).
- `AreaFormat`, `HaFormat` — yuza aniqligi.
- Ustun kengliklari va qator balandligi matn balandligiga proporsional.
