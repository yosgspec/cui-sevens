;全自動モード
(def AUTO_MODE false)
;プレイヤー人数
(def PLAYER_NUMBER 4)
;パス回数
(def PASS_NUMBER 3)

;トランプカードクラス
(def TrumpCard-suitStrs ["▲" "▼" "◆" "■" "Jo" "JO"])
(def TrumpCard-powerStrs ["Ａ" "２" "３" "４" "５" "６" "７" "８" "９" "10" "Ｊ" "Ｑ" "Ｋ" "KR"])
(def TrumpCard-suits 4)
(def TrumpCard-powers 13)
(defrecord TrumpCard[name suit power])
(defn make-TrumpCard[suit power]
	(let [name (str (nth TrumpCard-suitStrs suit) (nth TrumpCard-powerStrs power))]
		(TrumpCard. name suit power)
	)
)

;トランプの束クラス
(defprotocol ITrumpDeck
	(-count[self])
	(-shuffle[self])
	(draw[self])
)
(defrecord TrumpDeck[__g __deck] ITrumpDeck
	(-count[self]
		(count @__deck)
	)
	(-shuffle[self]
		(vswap! __deck shuffle)
	)
	(draw[self]
		(let [card (nth @__deck @__g)]
			(vswap! __g inc)
			card
		)
	)
)
(defn make-TrumpDack[& {:keys [jokers] :or {jokers 0}}]
	(let [
		deck (vec(concat
			(apply concat (for[suit (range TrumpCard-suits)]
				(for[power (range TrumpCard-powers)]
					(make-TrumpCard suit power)
				)
		))
		(for[jokers (range jokers)]
			(make-TrumpCard (+ TrumpCard-suits jokers) TrumpCard-powers)
		)))
	]
		(TrumpDeck. (volatile! 0) (volatile! deck))
	)
)

;プレイヤークラス
(defprotocol IPlayer
	(addCard[self card])
	(removeCard[self cardName])
	(existCard[self cardName])
	(gameOut[self])
)
(def MPlayer{
	:addCard(fn[self card]
		(vswap! (:deck self) #(cons card %))
	)
	:removeCard(fn[self cardName]
		(vswap! (:deck self) #(remove (fn[card] (= cardName (:name card))) %))
	)
	:existCard(fn[self cardName]
		(let [findIndex (first (keep-indexed (fn[index card] (when(= cardName (:name card)) index)) @(:deck self)))]
			(if findIndex findIndex -1)
		)
	)
	:gameOut(fn[self]
		(vreset! (:isGameOut self) true)
	)
})
(defprotocol ISortDeck(sortDeck[self]))
(def MSortDeck{
	:sortDeck (fn[self]
		(let [sortValue (fn[v] (+(*(:suit v) TrumpCard-powers) (:power v)))]
			(vswap! (:deck self) #(sort-by sortValue %))
		)
	)
})
(defrecord Player[deck id name isGameOut])
(extend Player
	IPlayer MPlayer
	ISortDeck MSortDeck)
(defn make-Player[id name]
	(Player. (volatile! '()) id name (volatile! false))
)

;トランプの場クラス
(defprotocol IView
	(view[self])
)
(def MView{
	:view(fn[self]
		(println (clojure.string/join (map :name @(:deck self))))
	)
})
(defprotocol IUseCard
	(useCard[self power][self player card])
)
(def MTrumpField-UseCard{
	:useCard(fn[self player card]
		(vswap! (:deck self) #(cons % card))
		(removeCard player (:name card))
	)
})

(defrecord TrumpField[deck __players])
(extend TrumpField
	IView MView
	ISortDeck MSortDeck
	IUseCard MTrumpField-UseCard)
(defn make-TrumpField[players]
	(TrumpField. (volatile! '()) players)
)

;七並べの列クラス
(defprotocol ISevensLine
	(rangeMin[self])
	(rangeMax[self])
)
(def SevensLine-__sevenIndex 6)
(def MSevensLine-UseCard{
	:useCard(fn[self power]
		(vreset! (nth (:cardLine self) power) true)
	)
})
(defprotocol ICheckUseCard
	(checkUseCard[self powerOrCard])
)
(def MSevensLine-CheckUseCard{
	:checkUseCard(fn[self power]
		(if(or
			(= power TrumpCard-powers)
			(= power (rangeMin self))
			(= power (rangeMax self))
		)
			true
			false
		)
	)
})
(defrecord SevensLine[cardLine] ISevensLine
	(rangeMin[self]
		(loop[i SevensLine-__sevenIndex](when(< -1 i)
			(if @(nth cardLine i) (recur(dec i)) i)
		))
	)
	(rangeMax[self]
		(loop[i SevensLine-__sevenIndex](when(< i TrumpCard-powers)
			(if @(nth cardLine i) (recur(inc i)) i)
		))
	)
)
(extend SevensLine
	IUseCard MSevensLine-UseCard
	ICheckUseCard MSevensLine-CheckUseCard)

(defn make-SevensLine[]
	(let [cardLine (vec(for[i (range TrumpCard-powers)] (volatile! false)))]
		(vreset! (nth cardLine SevensLine-__sevenIndex) true)
		(SevensLine. cardLine)
	)
)

;七並べクラス
(defprotocol ISevens
	(tryUseCard[self player card])
	(checkPlayNext[self player passes])
	(gameClear[self player])
	(gameOver[self player])
	(checkGameEnd[self])
	(result[self])
)

(def Sevens-__tenhoh 0xFF)

(defrecord Sevens[deck __players lines __rank clearCount]
	IUseCard
	(useCard[self player card]
		((:useCard MSevensLine-UseCard) (nth lines (:suit card)) (:power card))
		((:useCard MTrumpField-UseCard) self player card)
	)

	ICheckUseCard
	(checkUseCard[self card]
		((:checkUseCard MSevensLine-CheckUseCard) (nth lines (:suit card)) (:power card))
	)

	IView
	(view[self]
		(let [
			s (loop[i 0,s ""](if(< i TrumpCard-suits)
				(let [s (if(not= s "") (str s "\n") s)]
				(let [
					s (loop[n 0,s s,ss ""](if(< n TrumpCard-powers)
						(let [isCard @(nth (:cardLine (nth lines i)) n)]
							(recur
								(inc n)
								(str s (if isCard (nth TrumpCard-suitStrs i) "◇"))
								(str ss (if isCard (nth TrumpCard-powerStrs n) "◇"))
							)
						)
						(str s "\n" ss)
					))
				]
					(recur(inc i) s)
				))
				s
			))
		]
			(printf "%s\n\n" s)
		)
	)

	ISevens
	(tryUseCard[self player card]
		(if-not(checkUseCard self card)
			false
		(do
			(useCard self player card)
			true
		))
	)

	(checkPlayNext[self player passes]
		(if(< 0 passes)
			true
			(loop[i 0]
				(if(< i (count @(:deck player)))
					(if(checkUseCard self (nth @(:deck player) i))
						true
						(recur(inc i))
					)
					false
				)
			)
		)
	)

	(gameClear[self player]
		(vswap! clearCount inc)
		(vreset! (nth __rank (:id player)) @clearCount)
		(gameOut player)
	)

	(gameOver[self player]
		(vreset! (nth __rank (:id player)) -1)
		(vswap! (:deck player) #(loop[deck %](if-not(<= (count deck) 0)(do
			(useCard self player (first deck))
			(recur(rest deck))
		)
			deck
		)))
		(gameOut player)
	)

	(checkGameEnd[self]
		(loop[v __rank,isEnd true]
			(if isEnd
				(if(< 0 (count v))
					(do
						(recur (rest v) (not= 0 @(first v)))
					)
					true
				)
				false
			)
		)
	)

	(result[self]
		(println "\n【Game Result】")
		(loop[i 0](when(< i (count __rank))
			(let [rankStr
				(if(= @(nth __rank i) Sevens-__tenhoh)
					"天和"
				(if(< 0 @(nth __rank i))
					(str @(nth __rank i) "位")
					"GameOver..."
				))
			]
				(printf "%s: %s\n" (:name (nth __players i)) rankStr)
				(recur(inc i))
			)
		))
	)
)

(defn make-Sevens[players]
	(let [
		deck    (volatile! '())
		lines   (vec(for[i (range TrumpCard-suits)] (make-SevensLine)))
		rank    (vec(for[i (range (count players))] (volatile! 0)))
		clearCount (volatile! 0)
		players (vec players)
	]
	(let [self (Sevens. deck players lines rank clearCount)]
		(loop[i 0](when(< i TrumpCard-suits)
			(let [cardSevenName (str (nth TrumpCard-suitStrs i) (nth TrumpCard-powerStrs 6))]
				(loop[n 0]
					(when(< n (count players))
						(let [p (nth players n)]
						(let [cardSevenIndex (existCard p cardSevenName)]
							(if(< -1 cardSevenIndex)(do
								(let [card (nth @(:deck p) cardSevenIndex)]
									(printf "%s が%sを置きました。\n" (:name p) (:name card))
									(useCard self p card)
									(when(= 0 (count @(:deck p)))
										(printf "%s 【-- 天和 --】\n\n" (:name p))
										(vreset! (nth rank n) Sevens-__tenhoh)
										(gameOut p)
									)
								)
							)
								(recur (inc n))
							)
						))
					)
				)
				(recur(inc i))
			)
		))
		(println)
		self
	))
)

;カーソル選択モジュール
(defn SelectCursor[items]
(let [cursor (volatile! 0)]
(letfn [
	;カーソルの移動
	(move[x max]
		(vswap! cursor + x)
		(when(< @cursor 0) (vreset! cursor 0))
		(when(< (dec max) @cursor) (vreset! cursor (dec max)))
	)

	;カーソルの表示
	(view[]
		(let [select (map volatile! (repeat(count items) false))]
			(vreset! (nth select @cursor) true)
			(let [s (loop[i 0,s ""](if(< i (count items))
				(recur(inc i) (format (if @(nth select i) "%s[%s]" "%s%s") s (nth items i)))
				s
			))]
				(printf "%s\r" s)
			)
		)
	)
]

	(view)
	(loop[] (let [ch (System.Console/ReadKey true)]
		(if(= (.Key ch) ConsoleKey/Enter)
			(println)
		(do
			(when(= (.Key ch) ConsoleKey/LeftArrow) (move -1 (count items)))	;左
			(when(= (.Key ch) ConsoleKey/RightArrow) (move 1 (count items)))	;右
			(view)
			(recur)
		))
	))
	@cursor
)))


;七並べプレイヤークラス
(defprotocol ISevensPlayer
	(selectCard[self field])
)
(defrecord SevensPlayer[deck id name isGameOut passes]
	ISevensPlayer (selectCard[self field] 
		(when-not @isGameOut
		(if-not (checkPlayNext field self @passes)(do
			(gameOver field self)
			(view field)
			(printf "%s GameOver...\n\n" name)
		)
		(do
			(printf "【%s】Cards: %d Pass: %d\n" name (count @deck) @passes)
			(let [items (vec(map #(:name %) @deck))]
			(let [items (if(< 0 @passes) (conj items (str "PS:" @passes)) items)]
				(loop[cursor (SelectCursor items)]
					(println cursor)
					(if(and(< 0 @passes) (= cursor (dec(count items))))(do
						(vswap! passes dec)
						(view field)
						(printf "残りパスは%d回です。\n\n" @passes)
					)
					(if(tryUseCard field self (nth @deck cursor))(do
						(view field)
						(printf "俺の切り札!! >「%s」\n\n" (nth items cursor))
						(when(= 0 (count @deck))
							(printf "%s Congratulations!!\n\n" name)
							(gameClear field self)
						)
					)
					(do
						(println "そのカードは出せないのじゃ…\n")
						(recur(SelectCursor items))
					)))
				)
			))
		)))
	)
)
(extend SevensPlayer
	IPlayer MPlayer
	ISortDeck MSortDeck)

(defn make-SevensPlayer[id name passes]
	(SevensPlayer. (volatile! '()) id name (volatile! false) (volatile! passes))
)

;七並べAIプレイヤークラス
(defrecord SevensAIPlayer[deck id name isGameOut passes]
	ISevensPlayer (selectCard[self field] 
		(when-not @isGameOut
		(if-not (checkPlayNext field self @passes)(do
			(gameOver field self)
			(view field)
			(printf "%s> もうだめ...\n" name)
		)
		(do
			(printf "【%s】Cards: %d Pass: %d\n" name (count @deck) @passes)
			(let [items (vec(map #(:name %) @deck))]
			(let [items (if(< 0 @passes) (conj items (str "PS:" @passes)) items)]
				(print "考え中...\r")
				(System.Threading.Thread/Sleep 1000)

				(loop[cursor (rand-int (count items)),passCharge 0]
					(if(and(< 0 @passes) (= cursor (dec(count items))))(do
						(if(< passCharge 3)
							(recur(rand-int (count items)) (inc passCharge))
						(do
							(vswap! passes dec)
							(printf "パスー (残り%d回)\n\n" @passes)
						))
					)
					(if(tryUseCard field self (nth @deck cursor))(do
						(printf "これでも食らいなっ >「%s」\n\n" (nth items cursor))
						(when(= 0 (count @deck))
							(printf "%s> おっさき～\n\n" name)
							(gameClear field self)
						)
					)
						(recur(rand-int (count items)) passCharge)
					))
				)
			))
		)))
	)
)

(extend SevensAIPlayer
	IPlayer MPlayer
	ISortDeck MSortDeck)

(defn make-SevensAIPlayer[id name passes]
	(SevensAIPlayer. (volatile! '()) id name (volatile! false) (volatile! passes))
)

;メイン処理
(loop[i 0](when(< i 100)
	(println)
(recur(inc i))))
(println "
/---------------------------------------/
/                 七並べ                /
/---------------------------------------/
")

(let [
	trp (make-TrumpDack)
	p (volatile! '())
	pid (volatile! 0)
]
	(-shuffle trp)

	(when-not AUTO_MODE
		(print "NAME[Player]: ")
		(let [playerName (read-line)]
		(let [playerName (if(= "" playerName) "Player" playerName)]
			(vswap! p #(cons (make-SevensPlayer @pid playerName PASS_NUMBER) %))
			(vswap! pid inc)
		))
	)

	(loop[i 0](when(< i (- PLAYER_NUMBER (if AUTO_MODE 0 1)))
		(vswap! p #(cons (make-SevensAIPlayer @pid (str "CPU " (inc @pid)) PASS_NUMBER) %))
		(vswap! pid inc)
	(recur(inc i))))

	(let [p (reverse @p)]
		(loop[i 0](when(< i (-count trp))
			(addCard (nth p (mod i PLAYER_NUMBER)) (draw trp))
		(recur(inc i))))
		(doseq[pl p]
			(sortDeck pl)
		)
		(let [field (make-Sevens p)]
			(loop[]
				(view field)
				(let [selectLoop (loop[v p]
					(selectCard (first v) field)
					(let [endLoop (<= (count v) 1),isExitLoop (checkGameEnd field)]
						(if(or endLoop isExitLoop)
							(not isExitLoop)
							(recur(rest v))
						)
					)
				)]
					(when selectLoop (recur))
				)
			)
			(view field)
			(result field)
			(read-line)
		)
	)
)
