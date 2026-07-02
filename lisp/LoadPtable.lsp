;;; =====================================================================================
;;;  SalohiyatTable — Avtomatik yuklash (AutoLISP loader)
;;; =====================================================================================
;;;
;;;  O'RNATISH (bir marta):
;;;    1. Pastdagi SALOHIYAT_DLL_PATH qatorida DLL yo'lini TO'G'RILANG
;;;       (o'zingizning kompyuteringizdagi to'liq yo'l)
;;;    2. AutoCAD oching → APPLOAD
;;;    3. "Приложения" (Startup Suite) → "Добавить" → shu .lsp ni tanlang
;;;    4. Tayyor! AutoCAD har safar ochilganda plagin yuklanadi.
;;;
;;;  Buyruqlar:
;;;    PTABLE, PLTABLE          - koordinata jadvali
;;;    PTCHEGARA, PLCHEGARA     - chegaradoshlar jadvali
;;;    PTSOZLAMA                - sozlamalar oynasi
;;;    PTHAQIDA                 - dastur/mualliflar haqida
;;; =====================================================================================

;;; ===== SHU QATORNI O'ZGARTIRING: DLL ning TO'LIQ YO'LINI YOZING =====
(setq SALOHIYAT_DLL_PATH "C:\\SalohiyatTable.dll")
;;; =====================================================================

(defun salohiyat-autoload ( / dllpath)
  (setq dllpath SALOHIYAT_DLL_PATH)

  (if (findfile dllpath)
    (progn
      (command "._NETLOAD" dllpath)
      (princ (strcat "\n[SalohiyatTable] Yuklandi: " dllpath))
      (princ "\n[SalohiyatTable] Buyruqlar: PTABLE, PLTABLE, PTCHEGARA, PLCHEGARA, PTSOZLAMA, PTHAQIDA")
    )
    (princ (strcat "\n[SalohiyatTable] XATOLIK: DLL topilmadi: " dllpath
                   "\n  .lsp fayldagi SALOHIYAT_DLL_PATH qatorini tekshiring!"))
  )
  (princ)
)

;;; Avtomatik ishga tushadi:
(salohiyat-autoload)
