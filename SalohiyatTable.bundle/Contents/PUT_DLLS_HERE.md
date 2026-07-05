# Contents — bu yerga DLL fayllarini joylang

Build qilingandan so'ng quyidagi DLL'larni shu papkaga nusxalang:

| Fayl | Qayerdan | Qaysi AutoCAD uchun |
|------|----------|---------------------|
| `SalohiyatTable.dll` | `../src/bin/x64/Release/SalohiyatTable.dll` (net48 build) | 2018, 2019, 2020, 2021, 2022, 2023, 2024 + Mechanical 2021 |
| `SalohiyatTable_2025.dll` | `../build-2025/bin/Release/SalohiyatTable_2025.dll` (net8 build) | 2025+ |

`PackageContents.xml` AutoCAD versiyasiga qarab mos DLL'ni avtomatik tanlaydi.

> Agar siz faqat bitta AutoCAD versiyasida ishlatsangiz, faqat o'sha DLL yetarli —
> ikkinchisini qo'ymasangiz ham bundle ishlayveradi (mos versiya topilmasa, o'sha
> `<Components>` bloki e'tiborsiz qoldiriladi).
