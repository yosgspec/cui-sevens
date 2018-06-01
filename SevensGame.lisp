;全自動モード
(defconstant AUTO_MODE nil)
;プレイヤー人数
(defconstant PLAYER_NUMBER 4)
;パス回数
(defconstant PASS_NUMBER 3)

;コンストラクタ&メソッドマクロ
(defmacro definit (this (self &rest slots) &body body)
	`(defmethod initialize-instance :after ((,self ,this) &key)
		(with-slots ,slots ,self
			,@body)))
(defmacro defmet (funcname args this (self &rest slots) &body body)
	`(defmethod ,funcname ((,self ,this) ,@args)
		(with-slots ,slots ,self
			,@body)))
(defmacro defride (funcname args this (self &rest slots) &body body)
	`(defmethod ,funcname :around((,self ,this) ,@args)
		(with-slots ,slots ,self
			,@body)))

;トランプカードクラス
(defconstant TrumpCard-suitStrs #("▲" "▼" "◆" "■" "Jo" "JO"))
(defconstant TrumpCard-powerStrs #("Ａ" "２" "３" "４" "５" "６" "７" "８" "９" "10" "Ｊ" "Ｑ" "Ｋ" "KR"))
(defconstant TrumpCard-suits 4)
(defconstant TrumpCard-powers 13)
(defclass TrumpCard()(
	(name  :reader  :name)
	(suit  :reader  :suit
	       :initarg :suit)
	(power :reader  :power
	       :initarg :power)
)) 

(definit TrumpCard(self name suit power)
	(setf name (format nil "~A~A" (aref TrumpCard-suitStrs suit) (aref TrumpCard-powerStrs power)))
)

;トランプの束クラス
(defclass TrumpDeck()(
	(g      :initform 0)
	(deck   :initform '())
	(jokers :initarg  :jokers
	        :initform 0)
))

(defconstant TrumpDeck-rand (make-random-state t))

(definit TrumpDeck(self deck jokers) 
	(loop for suit from 0 below TrumpCard-suits do
		(loop for power from 0 below TrumpCard-powers do
			(push (make-instance 'TrumpCard :suit suit :power power) deck)
		)
	)

	(loop while (< 0 jokers) do
		(decf jokers)
		(push (make-instance 'TrumpCard :suit (+ TrumpCard-suits jokers) :power TrumpCard-powers) deck)
	)
)

(defmet -count() TrumpDeck(self deck)
	(length deck)
)

(defmet shuffle() TrumpDeck(self deck)
	(loop for i from 0 below (length deck) do
		(rotatef (nth i deck) (nth (random (- (length deck) i) TrumpDeck-rand) deck))
	)
)

(defmet draw() TrumpDeck(self g deck)
	(let ((card (nth g deck)))
		(incf g)
		card
	)
)

;プレイヤークラス
(defclass Player()(
	(deck      :reader   :deck
	           :initform '())
	(id        :reader   :id
	           :initarg  :id)
	(name      :reader   :name
	           :initarg  :name)
	(isGameOut :reader   :isGameOut
	           :initform nil)
))

(defmet sortDeck() Player(self deck)
	(flet ((sortValue(v) (+(*(:suit v) TrumpCard-powers) (:power v))))
		(setf deck (sort deck #'(lambda(a b) (< (sortValue a) (sortValue b)))))
	)
)

(defmet addCard((card TrumpCard)) Player(self deck)
	(push card deck)
)

(defmet removeCard(cardName) Player(self deck)
	(setf deck (delete-if #'(lambda(card) (string= cardName (:name card))) deck))
)

(defmet existCard(cardName) Player(self deck)
	(let ((existIndex (position cardName deck :test #'(lambda(cardName card) (string= cardName (:name card))))))
		(if existIndex existIndex -1)
	)
)

(defmet gameOut() Player(self isGameOut)
	(setf isGameOut t)
)

;トランプの場クラス
(defclass TrumpField()(
	(deck      :reader    deck
	           :initform '())
	(players   :initarg  :players)
))

(defmet useCard((player Player)  &optional card) TrumpField(self deck)
	(push card deck)
	(removeCard player (:name card))
)

(defmet view() TrumpField(self deck)
	(format nil "~{~A~^ ~}" (mapcar #'(lambda(d) (name d)) deck))
)

;七並べの列クラス
(defclass SevensLine()(
	(cardLine :reader   :cardLine
	          :initform (coerce(loop repeat TrumpCard-powers collect nil) 'vector))
))
(defconstant SevensLine-__sevenIndex 6)
(definit SevensLine(self cardLine)
	(setf (aref cardLine SevensLine-__sevenIndex) t)
)

(defmet rangeMin() SevensLine(self cardLine)
	(loop for i from SevensLine-__sevenIndex downto 0 do
		(unless(aref cardLine i)
			(return-from rangeMin i)
		)
	)
	0
)

(defmet rangeMax() SevensLine(self cardLine)
	(loop for i from SevensLine-__sevenIndex below TrumpCard-powers do
		(unless(aref cardLine i)
			(return-from rangeMax i)
		)
	)
	(1- TrumpCard-powers)
)

(defmet checkUseCardsl(power) SevensLine(self cardLine)
	(or
		(= power TrumpCard-powers)
		(= power (rangeMin self))
		(= power (rangeMax self))
	)
)
(defmet useCard(power &optional -nil) SevensLine(self cardLine)
	-nil
	(setf (aref cardLine power) t)
)

;七並べクラス
(defclass Sevens(TrumpField)(
	(lines      :initform (coerce(loop repeat TrumpCard-suits collect (make-instance 'SevensLine)) 'vector))
	(rank)
	(clearCount :reader   :clearCount
	            :initform 0)
))
(defconstant Sevens-__tenhoh #xFF)
(definit Sevens(self players lines rank)
	(setf rank(coerce(loop repeat (length players) collect 0) 'vector))
	(setf players (coerce players 'vector))
	(loop for i from 0 below TrumpCard-suits do
		(let ((cardSevenName (concatenate 'string (aref TrumpCard-suitStrs i) (aref TrumpCard-powerStrs 6))))
			(loop for n from 0 below (length players) do
				(let ((p (aref players n)))
				(let ((cardSevenIndex (existCard p cardSevenName)))
					(when(< -1 cardSevenIndex)
						(let ((card (nth cardSevenIndex (:deck p))))
							(format t "~A が~Aを置きました。~%" (:name p) (:name card))
							(useCard self p card)
							(when(= 0 (length(:deck p)))
								(format t "~A 【-- 天和 --】~%~%" (:name p))
								(setf (aref rank n) Sevens-__tenhoh)
								(gameOut p)
								(return)
							)
						)
					)
				))
			)
		)
	)
	(format t "~%")
)

(defride useCard((player Player) &optional card) Sevens(self lines)
	(useCard (aref lines (:suit card)) (:power card))
	(call-next-method)
)

(defmet checkUseCard(card) Sevens(self lines)
	(checkUseCardsl (aref lines (:suit card)) (:power card))
)

(defmet tryUseCard((player Player) card) Sevens(self)
	(unless(checkUseCard self card) (return-from tryUseCard nil))
	(useCard self player card)
	t
)

(defmet checkPlayNext((player Player) passes) Sevens(self)
	(when(< 0 passes) (return-from checkPlayNext t))
	(dolist(card (:deck player))
		(when(checkUseCard self card)
			(return-from checkPlayNext t)
		)
	)
	nil
)

(defmet gameClear((player Player)) Sevens(self clearCount rank)
	(incf clearCount)
	(setf (aref rank (:id player)) clearCount)
	(gameOut player)
)

(defmet gameOver((player Player)) Sevens(self rank)
	(setf (aref rank (:id player)) -1)
	(loop for i from (1-(length(:deck player))) downto 0 do
		(useCard self player (nth i (:deck player)))
	)
	(gameOut player)
)

(defmet checkGameEnd() Sevens(self rank)
	(loop for v across rank do
		(when(= 0 v) (return-from checkGameEnd nil))
	)
	t
)

(defmet view() Sevens(self lines cardLine)
	(let ((s ""))
		(loop for i from 0 below TrumpCard-suits do
			(let ((ss ""))
				(loop for n from 0 below TrumpCard-powers do
					(let ((isCard (aref (:cardLine (aref lines i)) n)))
						(setf s (format nil "~A~A" s (if isCard (aref TrumpCard-suitStrs i) "◇")))
						(setf ss (format nil "~A~A" ss (if isCard (aref TrumpCard-powerStrs n) "◇")))
					)
				)
				(setf s (format nil "~A~%~A~%" s ss))
			)
		)
		(format t "~A~%" s)
	)
)

(defmet result() Sevens(self players rank)
	(format t "~%【Game Result】~%")
	(loop for i from 0 below (length rank) do
		(let ((rankStr
			(if(= (aref rank i) Sevens-__tenhoh)
				"天和"
			(if(< 0 (aref rank i))
				(format nil "~d位" (aref rank i))
				"GameOver..."
			))
		))
			(format t "~A: ~A~%" (:name (aref players i)) rankStr)
		)
	)
)

;カーソル選択モジュール
(defun SelectCursor(items)
#+sbcl
	(progn(let ((s "")) 
		(dolist (v items)
			(setf s (format nil "~A ~A" s v))
		)
		(setf s (format nil "~A~%" s))
		(dotimes (i (length items))
			(setf s (format nil "~A [~2d]" s i))
		)
		(format t "~A~%> " s)(finish-output nil)
	)
	(loop
		(let ((cursor (or(parse-integer (read-line) :junk-allowed t) -1)))
			(if(< -1 cursor (length items))(progn
				(format t "SelectedItem: ~A~%" (elt items cursor))
				(return cursor)
			)(progn
				(format t "入力された値が不正です。~%> ")
				(finish-output nil)
			))
		)
	))

#+ccl
	(let ((cursor 0))
	(flet (
		;カーソルの移動
		(move(x max)
			(incf cursor x)
			(when(< cursor 0) (setf cursor 0))
			(when(< (1- max) cursor) (setf cursor (1- max)))
		)

		;カーソルの表示
		(view()
			(let (
				(select (loop for i from 0 below (length items) collect nil))
				(s "")
			)
				(setf (elt select cursor) t)
				(loop for i from 0 below (length items) do
					(setf s (format nil (if(elt select i) "~A[~A]" "~A~A") s (elt items i)))
				)
				(format t "~A~c" s #\return)
			)
		)
	)
		(view)
		(loop (let ((ch (#__getch)))
			(when(= ch #x0d)
				(format t "~%")
				(return cursor)
			)
			(when(= ch #xe0)
				(setf ch (#__getch))
				(when(= ch #x4b) (move -1 (length items)))	;左
				(when(= ch #x4d) (move 1 (length items)))	;右
			)
			(view)
		))
	))
)

;七並べプレイヤークラス
(defclass SevensPlayer(Player)(
	(passes :initarg :passes)
))

(defmet selectCard((field Sevens)) SevensPlayer(self deck name id isGameOut passes)
	(when isGameOut (return-from selectCard))
	(unless (checkPlayNext field self passes)
		(gameOver field self)
		(view field)
		(format t "~A GameOver...~%" name);
		(return-from selectCard)
	)
	(format t "【~A】Cards: ~d Pass: ~d~%" name (length deck) passes)
	(let ((items (mapcar #'(lambda(v) (:name v)) deck)))
		(when(< 0 passes) (setf items (nconc items (list(format nil "PS:~d" passes)))))
		(setf items (coerce items 'vector))

		(loop (let ((cursor (SelectCursor items)))
			(if(and(< 0 passes) (= cursor (1-(length items))))(progn
				(decf passes)
				(view field)
				(format t "残りパスは~d回です。~%~%" passes)
				(return)
			)
			(if(tryUseCard field self (nth cursor deck))(progn
				(view field)
				(format t "俺の切り札!! >「~A」~%" (aref items cursor))
				(when(= 0 (length deck))
					(format t "~A Congratulations!!~%~%" name)
					(gameClear field self)
				)
				(return)
			)
				(format t "そのカードは出せないのじゃ…~%~%")
			))
		))
	)
)

;七並べAIプレイヤークラス
(defclass SevensAIPlayer(SevensPlayer)())
(defmet selectCard((field Sevens)) SevensAIPlayer(self deck name id isGameOut passes)
	(when isGameOut (return-from selectCard))
	(unless (checkPlayNext field self passes)
		(gameOver field self)
		(view field)
		(format t "~A> もうだめ...~%" name);
		(return-from selectCard)
	)
	
	(format t "【~A】Cards: ~A Pass: ~A~%" name (length deck) passes)
	(let ((items (mapcar #'(lambda(v) (:name v)) deck)))
		(when(< 0 passes) (setf items (nconc items (list(format nil "PS:~d" passes)))))
		(setf items (coerce items 'vector))

		(format t "考え中...~A" #\return)
		;(sleep 1)
		(let ((passCharge 0))
			(loop (let ((cursor (random (length items))))
				(if(and(< 0 passes) (= cursor (1-(length items))))
					(if(< passCharge 3)
						(incf passCharge)
					(progn
						(decf passes)
						(format t "パスー (残り~d回)~%~%" passes)
						(return)
					))
				(when(tryUseCard field self (nth cursor deck))
					(format t "これでも食らいなっ >「~A」~%~%" (aref items cursor))
					(when(= 0 (length deck))
						(format t "~A> おっさき～~%~%" name)
						(gameClear field self)
					)
					(return)
				))
			))
		)
	)
)

;メイン処理
(loop for i from 0 below 100 do
	(format t "~%")
)

		(princ "
/---------------------------------------/
/                 七並べ                /
/---------------------------------------/


")

(let (
	(trp (make-instance 'TrumpDeck))
	(p '())
	(pid 0)
)
	(shuffle trp)

	(unless AUTO_MODE
		(princ "NAME[Player]: ")
		(let ((playerName (read-line)))
			(when (string= "" playerName) (setf playerName "Player"))

			(push (make-instance 'SevensPlayer :id pid :name playerName :passes PASS_NUMBER) p)
			(incf pid)
		)
	)

	(loop for i from 0 below (- PLAYER_NUMBER (if AUTO_MODE 0 1)) do
		(push (make-instance 'SevensAIPlayer :id pid :name (format nil "CPU ~d" (1+ i)) :passes PASS_NUMBER) p)
		(incf pid)
	)
	(setf p (coerce(nreverse p) 'vector))

	(loop for i from 0 below (-count trp) do
		(addCard (aref p (mod i PLAYER_NUMBER)) (draw trp))
	)
	(loop for v across p do
		(sortDeck v)
	)

	(let ((field (make-instance 'Sevens :players p)))
		(loop named selectLoop do
			(view field)
			(loop for v across p do
				(selectCard v field)
				(when(checkGameEnd field) (return-from selectLoop))
			)
		)

		(view field)
		(result field)
		(read-line)
		(princ " ")
	)
)
(quit)
