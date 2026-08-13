(defun c:EKLE ()
  (prompt "\nIcerigi degistirilecek textleri seciniz:")
  (setq p (ssget))
		(initget 1 "S B")
	  	(setq tip(getkword "\nEklenti Sona/Basa yapilacak:"))
  (setq eklenti (getstring T "\nEklenti icerigi:"))
  (if p
    (progn
      (setq n (sslength p))
      (setq l 0)
      (while (< l n)
	(if (= "TEXT" (cdr (assoc 0 (setq elist (entget (ssname p l))))))
	  (progn
	    (setq eskimetin (cdr (setq as (assoc 1 elist))))
					(if (= tip "S") (setq yenimetin(strcat eskimetin eklenti))) 
					(if (= tip "B") (setq yenimetin(strcat eklenti eskimetin)))
	    (setq elist (subst (cons 1 yenimetin) as elist))
	    (entmod elist)
	  )
	)				;if
	(setq l (1+ l))
      )					;while
    )					;progn
  )					;if
)