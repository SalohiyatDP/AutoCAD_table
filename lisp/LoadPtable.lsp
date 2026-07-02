;;; ===========================================================================
;;;  SalohiyatTable — avtomatik yuklovchi (Startup Suite uchun)
;;;
;;;  ENG ODDIY VA ISHONCHLI USUL:
;;;   1) Pastdagi *SalohiyatTableDllPath* qatoriga SalohiyatTable.dll ning
;;;      TO'LIQ yo'lini bir marta yozing (o'zingizdagi joyga moslang).
;;;      MUHIM: yo'lda '\' o'rniga '/' YOKI '\\' ishlating. Masalan:
;;;        "C:/Plugins/SalohiyatTable/SalohiyatTable.dll"
;;;        "C:\\Plugins\\SalohiyatTable\\SalohiyatTable.dll"
;;;   2) AutoCAD -> APPLOAD -> "Startup Suite" (Contents...) -> Add... ->
;;;      shu LoadPtable.lsp faylini qo'shing. Bir marta qo'shilsa yetadi.
;;;   3) Endi AutoCAD har ochilganda plagin avtomatik yuklanadi.
;;;
;;;  Yuklangandan keyingi buyruqlar:
;;;    PTABLE, PLTABLE           - koordinata jadvali
;;;    PTCHEGARA, PLCHEGARA      - chegaradoshlar jadvali
;;;    PTSOZLAMA                 - sozlamalar oynasi
;;;    PTHAQIDA                  - dastur/mualliflar haqida
;;; ===========================================================================

;; >>>>>>>>>>>>>>>>>>  SHU YO'LNI O'ZGARTIRING  <<<<<<<<<<<<<<<<<<
(setq *SalohiyatTableDllPath* "C:/Plugins/SalohiyatTable/SalohiyatTable.dll")
;; >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

(defun c:PTLOAD ( / dll )
  ;; Avval to'liq yo'l, topilmasa Support papkadan zaxira sifatida qidiramiz
  (setq dll (findfile *SalohiyatTableDllPath*))
  (if (not dll) (setq dll (findfile "SalohiyatTable.dll")))

  (cond
    ((not dll)
     (princ "\nXATO: SalohiyatTable.dll topilmadi.")
     (princ "\nLoadPtable.lsp ichidagi *SalohiyatTableDllPath* yo'lini tekshiring.")
    )
    (*SalohiyatTableLoaded*
     (princ "\nSalohiyatTable allaqachon yuklangan.")
    )
    (T
     (command "_.NETLOAD" dll)
     (setq *SalohiyatTableLoaded* T)
     (princ (strcat "\nSalohiyatTable yuklandi: " dll))
     (princ "\nBuyruqlar: PTABLE, PLTABLE, PTCHEGARA, PLCHEGARA, PTSOZLAMA, PTHAQIDA")
    )
  )
  (princ)
)

;; Startup Suite orqali yuklanganda avtomatik ishga tushadi
(c:PTLOAD)
(princ)
