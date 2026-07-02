;;; ===========================================================================
;;;  SalohiyatDP - AutoCAD_table plagini yuklovchisi
;;;  SalohiyatTable.dll ni NETLOAD orqali yuklaydi.
;;;
;;;  Foydalanish:
;;;    1) SalohiyatTable.dll ni AutoCAD "Support File Search Path" papkalaridan
;;;       biriga joylang (yoki shu .lsp fayl bilan bir papkaga).
;;;    2) Ushbu faylni APPLOAD orqali yuklang (yoki Startup Suite'ga qo'shing).
;;;    3) Buyruq qatoriga PTLOAD deb yozing (yoki fayl avtomatik yuklanadi).
;;;
;;;  Yuklangandan so'ng buyruqlar:
;;;    PTABLE  - nuqtalarni ketma-ket ko'rsatib jadval yasash
;;;    PLTABLE - poliliniya cho'qqilaridan jadval yasash
;;; ===========================================================================

(defun c:PTLOAD ( / dll )
  (setq dll (findfile "SalohiyatTable.dll"))
  (if dll
    (progn
      (command "_.NETLOAD" dll)
      (princ "\nSalohiyatTable.dll yuklandi. Buyruqlar: PTABLE, PLTABLE")
    )
    (progn
      (princ "\nSalohiyatTable.dll topilmadi.")
      (princ "\nDLL ni Support papkasiga joylang yoki APPLOAD -> NETLOAD orqali yuklang.")
    )
  )
  (princ)
)

;; Fayl yuklanishi bilan DLL ni ham avtomatik yuklashga urinish.
(c:PTLOAD)
(princ "\nSalohiyatTable yuklovchisi tayyor. PTLOAD - qayta yuklash uchun.")
(princ)
