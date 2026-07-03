# SalohiyatTable — AutoCAD geodeziya plagini

AutoCAD uchun geodeziya/kadastr ishlarini tezlashtiruvchi plagin. Nuqtalarni raqamlab
koordinata jadvali yasaydi, chegaradoshlar jadvalini tuzadi, GPS nuqtalaridan poligon
quradi, tomorqa yerini ajratadi, devor belgisini chizadi va poligon bo'yicha chizmani
ajratib (kesib) oladi.

Plagin ikki qismdan iborat:
- **`SalohiyatTable.dll`** — asosiy mantiq (AutoCAD .NET / ObjectARX API, C#).
- **`LoadPtable.lsp`** — DLL ni `NETLOAD` orqali yuklovchi AutoLISP fayli.

Barcha sozlamalar `%APPDATA%\SalohiyatTable\settings.xml` faylida **saqlanadi**
(keyingi seanslarda ham qoladi) va `PTSOZLAMA` oynasi orqali tahrirlanadi.

---

## Buyruqlar

| Buyruq | Vazifasi |
|--------|----------|
| `PTABLE`    | Nuqtalarni ketma-ket ko'rsatib **koordinata jadvali** yasash. |
| `PLTABLE`   | **Poliliniya** (LWPOLYLINE) cho'qqilaridan koordinata jadvali yasash. |
| `PTCHEGARA` | Nuqtalardan **chegaradoshlar jadvali**. |
| `PLCHEGARA` | Poliliniyadan **chegaradoshlar jadvali**. |
| `PTPOLIGON` | GPS nuqta raqamlari bo'yicha **poligon** (yopiq poliliniya) yasash. |
| `PTTOMORQA` | Yopiq maydondan ichkariga surilgan **Tomorqa** poligoni + "Tomorqa" yozuvi. |
| `PTDEVOR`   | **Devor belgilash**: devor bo'ylab poliliniya + chiziq turi/masshtab. |
| `PTAJRAT`   | **Poligondan ajratish**: ichki poligon ichini qoldirib, atrofini tozalash. |
| `PTSOZLAMA` | **Sozlamalar** oynasi (tablarga bo'lingan). |
| `PTHAQIDA`  | Plagin va **mualliflar** haqida. |
| `PTLOAD`    | DLL ni qayta yuklash (LISP funksiyasi). |

---

## Lentadagi menyu (Ribbon)

Plagin yuklangach, AutoCAD lentasida **"SalohiyatTable"** yorlig'i paydo bo'ladi:

| Panel | Tugmalar |
|-------|----------|
| **Jadval** | Nuqtalardan jadval (`PTABLE`), Poliliniyadan jadval (`PLTABLE`) |
| **Chegaradoshlar** | Chegaradoshlar nuqtalardan (`PTCHEGARA`), poliliniyadan (`PLCHEGARA`) |
| **Poligon** | Nuqtalardan poligon (`PTPOLIGON`), Tomorqa yaratish (`PTTOMORQA`), Poligondan ajratish (`PTAJRAT`) |
| **Devor** | Devor belgilash (`PTDEVOR`) |
| **Sozlamalar** | Sozlamalar (`PTSOZLAMA`), Haqida (`PTHAQIDA`) |

---

## 1. Koordinata jadvali (`PTABLE` / `PLTABLE`)

Belgilangan nuqtalarni raqamlab, koordinata va masofa jadvalini yasaydi. Jadval ostiga
**yer maydoni** (m.kv va gektar) va **chegara uzunligi** (metr) yoziladi.

| Nuqtalar № | Geo ma'lumotlar |||
|:---:|:---:|:---:|:---:|
|      | **Uzunligi(m)** | **X** | **Y** |
| 1 |     | X1 | Y1 |
|   | d(1→2) |    |    |
| 2 |     | X2 | Y2 |
| … |   … | …  | …  |
| 1 |     | X1 | Y1 |

```
Yer maydoni: <yuza> m.kv (<gektar> ga)
Chegara uzunligi: <perimetr> m
```

- Har bir **nuqta qatori** — raqam, X, Y koordinatalar.
- Har bir **masofa qatori** — shu nuqtadan keyingi nuqtagacha masofa (metr).
- Yopiq kontur uchun oxirida **1-nuqta** takrorlanib, kontur yopiladi.
- Koordinatalar chizma birligida (odatda **metr**) olinadi.
- Tartib raqamlari har doim **poligondan tashqariga** joylashtiriladi.
- Ustun kengliklari eng uzun matnga qarab **avtomatik moslashadi**.
- `PTABLE`/`PLTABLE` faqat **jadval joyini** so'raydi; matn balandligi, o'nlik xonalar,
  nuqta belgisi va jadval burchagi **sozlamalardan** olinadi.

**Sozlamalar → "Jadval" tabi:** Matn balandligi, O'nlik xonalar soni, Nuqta belgisi
(Hech / Doira / X), Jadval burchagi (ko'rsatilgan nuqta jadvalning qaysi burchagi bo'lishi).

---

## 2. Chegaradoshlar jadvali (`PTCHEGARA` / `PLCHEGARA`)

Jadval ustida **Ijrochi** (sozlamalardan) va **Buyurtmachi** satrlari chiziladi.
Jadvalga faqat raqamlar qo'yiladi; "Chegaradoshlar" ustuni qo'lda to'ldirish uchun bo'sh
qoladi (tag chiziq `___` bilan).

```
Ijrochi: <sozlamalardan>            __________________________
Buyurtmachi: __________________________________________________
┌───────────────────────────────────────────────────────────┐
│      Yer uchastkasining chegara burulish nuqtalari tasnifi  │
├───────────────────┬─────────────────────────────────────────┤
│ Burulish nuqtalari│                                         │
├────────┬──────────┤            Chegaradoshlar               │
│  dan   │  gacha   │                                         │
├────────┼──────────┼─────────────────────────────────────────┤
│   1    │    2     │ ______________                          │
│   2    │    3     │ ______________                          │
│  ...   │   ...    │ ______________                          │
└────────┴──────────┴─────────────────────────────────────────┘
```

**Sozlamalar → "Chegaradoshlar" tabi:**
- **Chegaradosh burchagi** — bu jadval qaysi burchakdan qo'yiladi (asosiy jadvaldan mustaqil).
- **Chegaradosh matn balandligi** — alohida matn balandligi.
- **Chegaradosh ustuni (×h)** — "Chegaradoshlar" ustuni kengligi (matn balandligiga nisbatan).
- **Ijrochi** — ijrochi ismi (jadval ustida chiqadi).

Buyurtmachi satri va har bir "Chegaradoshlar" katagi tag chiziq bilan chiziladi —
ustiga ikki marta bosib (double-click) qo'lda yozish oson. Sarlavha qatorlari balandligi
o'ralgan matnga moslashadi.

---

## 3. Nuqtalardan poligon (`PTPOLIGON`)

GPS qurilmasidan olingan nuqtalar chizmada odatda **`POINT` obyekti** + yonida **tartib
raqami** yozilgan matn ko'rinishida bo'ladi. `PTPOLIGON` kiritilgan raqamli nuqtalarni
ketma-ket tutashtirib **yopiq poligon** yasaydi.

Ishlatish:
1. `PTPOLIGON` buyrug'ini yozing.
2. Raqamlarni kiriting, masalan: `145-165, 171, 182-260`
   - `a-b` — diapazon (`145-165` = 145, 146, …, 165).
   - Vergul bilan ajratiladi; alohida raqam ham bo'ladi (`171`).
3. Nuqtalar berilgan tartibda tutashtiriladi; topilmagan raqamlar buyruq qatorida ko'rsatiladi.

> Raqamli matn (sof butun son, masalan `757`) o'ziga **eng yaqin `POINT`** ga bog'lanadi;
> o'nlik balandlik (`874.77`) yoki matnli tavsif raqam deb olinmaydi. Nuqtalar atributli
> blok bo'lsa — butun sonli atribut raqam sifatida ishlatiladi.
> Yasalgan poligonni keyin `PLTABLE` yoki `PLCHEGARA` bilan jadvalga aylantirish mumkin.

---

## 4. Tomorqa yeri (`PTTOMORQA`)

Yopiq maydonning tashqi chegarasidan **ichkariga** ma'lum masofaga surilgan **nuqtali
(DOT)** poligon chizadi va markaziga **"Tomorqa"** deb yozadi.

Ishlatish:
1. `PTTOMORQA` buyrug'ini yozing.
2. Tomorqa maydoni **ichidan** bitta nuqta ko'rsating (chegara avtomatik aniqlanadi).

**Sozlamalar → "Tomorqa" tabi:**
- **Ichkariga (m)** — tashqi chegaradan ichkariga masofa (standart `2`).
- **Matn balandligi** — "Tomorqa" yozuvi balandligi (standart `2.5`).
- **Burchaklari** — `Qirrali` (o'tkir) yoki `Yoysimon` (yumaloqlangan).
- **Yoy radiusi** — `Yoysimon` tanlanganda burchak yoyi radiusi.
- **Chiziq masshtabi** — nuqtali chiziq masshtabi (LTSCALE); nuqtalar ko'rinmasa o'zgartiring.

> Nuqta ko'rsatilgan joy atrofi to'liq **yopiq** bo'lishi kerak. Masofa maydonga nisbatan
> juda katta bo'lsa, ichki poligon yasalmaydi (ogohlantiradi).

---

## 5. Devor belgilash (`PTDEVOR`)

Devor bo'ylab poliliniya chizadi va unga devor chiziq turi + masshtabini qo'llab,
**devor belgisi** hosil qiladi. Ixtiyoriy ravishda ikkinchi parallel chiziq ham chizadi.

Ishlatish:
1. `PTDEVOR` buyrug'ini yozing.
2. Devor bo'ylab nuqtalarni ketma-ket ko'rsating, tugatish uchun **Enter**.

**Sozlamalar → "Devor" tabi:**
- **Chiziq turi (linetype)** — devor belgisini beruvchi chiziq turi (masalan `ОГРАДА_ГЛ`).
  Bo'sh qoldirilsa joriy/BYLAYER ishlatiladi.
- **Chiziq masshtabi** — chiziq turi masshtabi (standart `0.3`).
- **Qatlam (layer)** — poliliniya qo'yiladigan qatlam (bo'sh = joriy; yo'q bo'lsa yaratiladi).
- **Devor eni (masofa)** — asosiy chiziqdan **ikkinchi parallel chiziq**gacha masofa
  (`0` = ikkinchi chiziq yo'q). Ikkinchi chiziq shtrix tomoniga qo'yilsa, shtrixlar ikki
  chiziq orasida devor ko'rinishini beradi.
- **Ikkinchi chiziqni qarama-qarshi tomonga** — ikkinchi chiziq tomonini almashtiradi.

> Maxsus chiziq turi (`acad.lin` da yo'q) chizmada allaqachon yuklangan bo'lishi kerak.
> Aks holda poliliniya joriy chiziq turida chiziladi (buyruq qatorida eslatma chiqadi) —
> bunda **Qatlam** ni o'sha chiziq turi biriktirilgan qatlamga qo'yib, BYLAYER'dan foydalaning.

---

## 6. Poligondan ajratish (`PTAJRAT`)

Ichki poligon ichini qoldirib, uning tashqarisidagi (tashqi poligongacha bo'lgan) chizma
va yozuvlarni tozalaydi. Masalan bitta binoni ajratib olish uchun.

Ishlatish:
1. Avval **ichki** va **tashqi** yopiq poliliniyalarni chizib oling (`PLINE` / `RECTANG`).
2. `PTAJRAT` buyrug'ini yozing.
3. **Ichki** poliliniyani tanlang (qoldiriladigan soha).
4. **Tashqi** poliliniyani tanlang (tozalash chegarasi).

Natija (tashqi poligon ichidagi obyektlar bo'yicha):
- Ichki poligon **ichidagi** qismlar **qoladi**.
- Ichki poligon **tashqarisidagi** qismlar **o'chiriladi**.
- Ichki chegarani **kesib o'tgan** chiziq/poliliniyalar chegara bo'ylab **kesiladi**
  (ichki bo'lagi qoladi). Kesishuv 2D da (Z'ga bog'liqsiz) hisoblanadi.
- Matn/blok kabi obyektlar joylashuviga qarab qoladi yoki o'chadi.
- Tashqi poligondan **tashqaridagilar** (va tashqi chegarani kesib chiquvchilar) **tegilmaydi**.
- Ikki poligonning o'zi o'chirilmaydi (kerak bo'lsa qo'lda o'chirasiz).

---

## Sozlamalar (`PTSOZLAMA`)

Sozlamalar oynasi tablarga bo'lingan: **Jadval / Chegaradoshlar / Tomorqa / Devor**.
Har bir funksiya sozlamasi o'z tabida. "Saqlash" bosilganda diskka yoziladi va keyingi
AutoCAD seanslarida ham qoladi.

**Mualliflar** ma'lumoti jadvalda emas — `PTHAQIDA` ("Haqida") oynasida ko'rsatiladi.

---

## Kompilyatsiya (Build)

Talablar:
- Windows + o'rnatilgan **AutoCAD** (2019+).
- **.NET Framework 4.8** SDK va **Visual Studio 2019/2022** yoki `dotnet` / `msbuild`.

Loyiha AutoCAD .NET API (`acmgd`, `acdbmgd`, `accoremgd`) ga **NuGet** paketi `AutoCAD.NET`
orqali bog'lanadi — AutoCAD papkasini qo'lda ko'rsatish shart emas.

### 1-qadam: AutoCAD versiyangizni tanlang

`src/SalohiyatTable.csproj` faylidagi `AcadNetVersion` ni o'z versiyangizga (yoki pastroq) moslang:

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

### 2-qadam: Build

**Visual Studio orqali:**
1. `SalohiyatTable.sln` ni oching (internet bo'lsin — NuGet Restore uchun).
2. Konfiguratsiya = **Release**, platforma = **x64**.
3. **Build → Build Solution**.

**Buyruq qatoridan:**
```powershell
dotnet build SalohiyatTable.sln -c Release
```

Natija: `src\bin\Release\SalohiyatTable.dll`.

### Lentadagi menyu haqida (muhim)

Menyu tugmalari `AdWindows.dll` ga bog'liq (u NuGet'da yo'q, faqat AutoCAD papkasida bor).
Shuning uchun **menyu chiqishi uchun** `csproj` dagi **`AutoCADPath`** ni o'z AutoCAD
papkangizga to'g'rilang (masalan `C:\Program Files\Autodesk\AutoCAD 2021`) va qayta build qiling.

- `AutoCADPath` to'g'ri bo'lsa → lenta menyusi qo'shiladi.
- Ko'rsatilmasa/noto'g'ri bo'lsa → build baribir muvaffaqiyatli bo'ladi, lekin menyu
  bo'lmaydi; buyruqlarni (`PTABLE` va h.k.) qo'lda yozib ishlatasiz.
- Menyu ko'rinmasa: lenta yoqilganini (`RIBBON`) va DLL `NETLOAD` qilinganini tekshiring.

---

## O'rnatish (Startup Suite — eng oddiy usul)

DLL yo'li `.lsp` faylga **bir marta** yoziladi va fayl **Startup Suite** ga qo'shiladi.
Keyin AutoCAD har ochilganda plagin avtomatik yuklanadi.

1. `SalohiyatTable.dll` va `LoadPtable.lsp` ni doimiy papkaga joylang (masalan `C:\Plugins\SalohiyatTable\`).
2. `LoadPtable.lsp` ni oching va DLL yo'lini moslang:
   ```lisp
   (setq SALOHIYAT_DLL_PATH "C:\\SalohiyatTable.dll")
   ```
   > Yo'lda `\` o'rniga `\\` yoki `/` ishlating.
3. AutoCAD → `APPLOAD` → **Startup Suite** (Приложения) → **Add...** → `LoadPtable.lsp` → **Close**.
4. AutoCAD'ni qayta ishga tushiring.

> DLL ni qayta build qilsangiz ham, `.lsp` ni Startup Suite ga qayta qo'shish shart emas.
> **Muqobil yo'l:** har safar `APPLOAD` orqali DLL ni qo'lda `NETLOAD` qilish.

---

## Loyiha tuzilishi

```
AutoCAD_table/
├── SalohiyatTable.sln           # Visual Studio solution (Release|x64)
├── src/
│   ├── SalohiyatTable.csproj    # .NET loyiha fayli
│   ├── Commands.cs              # PTABLE / PLTABLE / PTCHEGARA / PLCHEGARA / PTSOZLAMA / PTHAQIDA
│   ├── RibbonUi.cs              # lentadagi menyu (Ribbon tab + panellar)
│   ├── PluginSettings.cs        # saqlanadigan sozlamalar (XML) + plagin ma'lumoti
│   ├── SettingsForm.cs          # sozlamalar oynasi (WinForms, tablar)
│   ├── AboutForm.cs             # "Haqida" oynasi (mualliflar)
│   ├── PointCollector.cs        # nuqta yig'ish (interaktiv / poliliniya)
│   ├── MarkerDrawer.cs          # nuqta belgisi va raqamlarini chizish
│   ├── TableBuilder.cs          # koordinata jadvali + yuza/perimetr matni
│   ├── NeighborsTableBuilder.cs # chegaradoshlar jadvali
│   ├── PolygonFromPoints.cs     # PTPOLIGON - nuqta raqamlaridan poligon
│   ├── TomorqaBuilder.cs        # PTTOMORQA - tomorqa yeri
│   ├── DevorMarker.cs           # PTDEVOR - devor belgilash
│   ├── PolygonCrop.cs           # PTAJRAT - poligondan ajratish (kesish)
│   ├── GeometryHelper.cs        # masofa, perimetr, yuza (Shoelace)
│   └── TableOptions.cs          # o'lcham/format sozlamalari
├── lisp/
│   └── LoadPtable.lsp           # NETLOAD yuklovchisi (Startup Suite uchun)
├── .gitignore
└── README.md
```

---

## Mualliflar

- Topograf: Abdujabborov Sherzod Jahongir o'g'li
- Topograf: Karimbekov Asadbek Nasibbek o'g'li
- Tashkilot: Davlat Kadastrlari Palatasi, Kosonsoy tuman filiali
