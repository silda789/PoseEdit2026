(defun c:rbk (/ blksecimi blkname blknew)
; select block to view name
(setq blksecimi (entget(car(entsel "Select block to view NAMES : ")))
       blkname (cdr(assoc 2 blksecimi)) )
(princ (strcat ">>> " (cdr (assoc 2 blksecimi)) " <<<" )) ; princ block name
(setq blknew (getstring t "\nEnter new block name: "))
 (command "_.rename" "_block" blkname blknew)
 (prompt "\nBlock ismi baþarýyla deðiþtirilmiþtir...")
 (princ)
 )