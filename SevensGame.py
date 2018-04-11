import random

#全自動モード
AUTO_MODE=False
#プレイヤー人数
PLAYER_NUMBER=4
#パス回数
PASS_NUMBER=3

#トランプカードクラス
class TrumpCard:
	suitStrs=["▲","▼","◆","■","Jo","JO"]
	powerStrs=["Ａ","２","３","４","５","６","７","８","９","10","Ｊ","Ｑ","Ｋ","KR"]
	suits=4
	powers=13

	def __init__(self,suit,power):
		self.name=TrumpCard.suitStrs[suit]+TrumpCard.powerStrs[power]
		self.power=power
		self.suit=suit

#トランプの束クラス
class TrumpDeck:
	def trumpIter(self,deck):
		for v in self.deck:
			yield v

	@property
	def count(self):
		return len(self.deck)

	def __init__(self):
		self.deck=[]
		for suit in range(TrumpCard.suits):
			for power in range(TrumpCard.powers):
				self.deck.append(TrumpCard(suit,power))

		""" Joker
		deck.Add(New TrumpCard(4,TrumpCard.powers))
		deck.Add(New TrumpCard(5,TrumpCard.powers))
		"""

		self.__g=self.trumpIter(self.deck)

	def shuffle(self):
		random.shuffle(self.deck)

	def draw(self):
		return next(self.__g)

#プレイヤークラス
class Player:
	def __init__(self,id,name):
		self.deck=[]
		self.id=id
		self.name=name
		self.isGameOut=False

	def sortRefDeck(deck):
		sortValue=lambda v: v.suit*TrumpCard.powers+v.power
		deck.sort(key=sortValue)
	
	def sortDeck(self): Player.sortRefDeck(self.deck)

	def addCard(self,card):
		self.deck.append(card)

	def removeCard(self,cardName):
		self.deck.pop([v.name for v in self.deck].index(cardName))

	def existCard(self,cardName):
		try:
			return [v.name for v in self.deck].index(cardName)
		except ValueError:
			return -1

	def gameOut(self):
		self.isGameOut=True

#トランプの場クラス
class TrumpField:
	def __init__(self,players):
		self.deck=[]
		self._players=players

	def sortDeck(self):
		Player.sortRefDeck(deck)

	def useCard(self,player,card):
		self.deck.append(card)
		player.removeCard(card.name)

	def view(self):
		print(" ".join([v.name for v in self.deck]))

#七並べの列クラス
class SevensLine:
	__sevenIndex=6

	def __init__(self):
		self.cardLine=[False for i in range(TrumpCard.powers)]
		self.cardLine[SevensLine.__sevenIndex]=True

	def rangeMin(self):
		for i in range(SevensLine.__sevenIndex,-1,-1):
			if not self.cardLine[i]: return i
		return i

	def rangeMax(self):
		for i in range(SevensLine.__sevenIndex,TrumpCard.powers):
			if not self.cardLine[i]: return i
		return i

	def checkUseCard(self,power):
		if(
			power==TrumpCard.powers or
			power==self.rangeMin() or 
			power==self.rangeMax()
		):
			return True
		else:
			return False

	def useCard(self,power):
		self.cardLine[power]=True

#七並べクラス 
class Sevens(TrumpField):
	__tenhoh=0xFF

	def __init__(self,players):
		super().__init__(players)
		self.lines=[SevensLine() for i in range(TrumpCard.suits)]
		self.__rank=[0 for i in self._players]
		self.clearCount=0

		for i in range(TrumpCard.suits):
			cardSevenName=TrumpCard.suitStrs[i]+TrumpCard.powerStrs[6]
			for n in range(len(self._players)):
				p=self._players[n]
				cardSevenIndex=p.existCard(cardSevenName)
				if -1<cardSevenIndex:
					card=p.deck[cardSevenIndex]
					print(f"{p.name} が{card.name}を置きました。")
					self.useCard(p,card)
					if len(p.deck)==0:
						print(f"{p.name} 【-- 天和 --】\n")
						rank[n]=tenhoh
						p.gameOut()
					break
		print()

	def useCard(self,player,card):
		self.lines[card.suit].useCard(card.power)
		super().useCard(player,card)

	def checkUseCard(self,card):
		return self.lines[card.suit].checkUseCard(card.power)

	def tryUseCard(self,player,card):
		if not self.checkUseCard(card): return False
		self.useCard(player,card)
		return True

	def checkPlayNext(self,player,passes):
		if 0<passes: return True
		for card in player.deck:
			if self.checkUseCard(card):
				return True
		return False

	def gameClear(self,player):
		self.clearCount+=1
		self.__rank[player.id]=self.clearCount
		player.gameOut()

	def gameOver(self,player):
		self.__rank[player.id]=-1
		for i in range(len(player.deck)-1,-1,-1):
			self.useCard(player,player.deck[i])
		player.gameOut()

	def checkGameEnd(self):
		for v in self.__rank:
			if v==0: return False
		return True

	def view(self):
		s=""
		for i in range(TrumpCard.suits):
			ss=""
			for n in range(TrumpCard.powers):
				if self.lines[i].cardLine[n]:
					s+=TrumpCard.suitStrs[i]
					ss+=TrumpCard.powerStrs[n]
				else:
					s+="◇"
					ss+="◇"
			s+="\n"+ss+"\n"
		print(s)

	def result(self):
		print("\n【Game Result】")
		for i in range(len(self.__rank)):
			if self.__rank[i]==Sevens.__tenhoh:
				rankStr="天和"
			elif 0<self.__rank[i]:
				rankStr=f"{self.__rank[i]}位"
			else:
				rankStr="GameOver..."
			print(f"{self._players[i].name}: {rankStr}")

#カーソル選択モジュール
def SelectCursor(items):
	cursor=0
	#カーソルの移動
	def move(x,max):
		nonlocal cursor
		cursor+=x
		if cursor<0: cursor=0
		if max-1<cursor: cursor=max-1

	#カーソルの表示
	def view():
		nonlocal items,cursor
		select=[False for i in items]
		select[cursor]=True
		s=""
		for i in range(len(items)):
			s+=f"[{items[i]}]" if select[i] else items[i]
		print(f"{s}\r",end="")

	view()
	from msvcrt import getch
	while True:
		ch=ord(getch())
		if ch==0x0d:
			print()
			break

		if ch==0xe0:
			ch=ord(getch())
			if ch==0x4b: move(-1,len(items))	#左
			if ch==0x4d: move(1,len(items))		#右

		view()
	return cursor

#七並べプレイヤークラス
class SevensPlayer(Player):
	def __init__(self,id,name,passes):
		super().__init__(id,name)
		self._passes=passes

	def selectCard(self,field):
		if self.isGameOut: return
		if not field.checkPlayNext(self,self._passes):
			field.gameOver(self)
			field.view()
			print(f"{self.name} GameOver...\n")
			return

		print(f"【{self.name}】Cards: {len(self.deck)} Pass: {self._passes}")
		items=[v.name for v in self.deck]
		if 0<self._passes: items.append(f"PS:{self._passes}")

		while True:
			cursor=SelectCursor(items)

			if 0<self._passes and len(items)-1==cursor:
				self._passes-=1
				field.view()
				print(f"残りパスは{self._passes}回です。\n")
				break

			elif field.tryUseCard(self,self.deck[cursor]):
				field.view()
				print(f"俺の切り札!! >「{items[cursor]}」\n")
				if len(self.deck)==0:
					print(f"{self.name} Congratulations!!\n")
					field.gameClear(self)
				break

			else:
				print(f"そのカードは出せないのじゃ…\n")
				continue

#七並べAIプレイヤークラス
class SevensAIPlayer(SevensPlayer):
	def __init__(self,id,name,passes):
		super().__init__(id,name,passes)

	def selectCard(self,field):
		if self.isGameOut: return
		if not field.checkPlayNext(self,self._passes):
			field.gameOver(self)
			field.view()
			print(f"{self.name}> もうだめ...\n")
			return

		print(f"【{self.name}】Cards: {len(self.deck)} Pass: {self._passes}")
		items=[v.name for v in self.deck]
		if 0<self._passes: items.append(f"PS:{self._passes}")

		print("考え中...",end="\r")
		import time
		time.sleep(1)

		passCharge=0
		while True:
			cursor=random.randrange(len(items))

			if 0<self._passes and len(items)-1==cursor:
				if passCharge<3:
					passCharge+=1
					continue

				self._passes-=1
				print(f"パスー (残り{self._passes}回)\n")
				break

			elif field.tryUseCard(self,self.deck[cursor]):
				print(f"これでも食らいなっ >「{items[cursor]}」\n")
				if len(self.deck)==0:
					print(f"{self.name}> おっさき～\n")
					field.gameClear(self)
				break

			else: continue

#メイン処理
if __name__=="__main__":
	for i in range(100):
		print()

	print(
"""/---------------------------------------/
/                 七並べ                /
/---------------------------------------/

""")
	trp=TrumpDeck()
	trp.shuffle()

	p=[]
	pid=0
	if not AUTO_MODE:
		p.append(SevensPlayer(pid,"Player",PASS_NUMBER))
		pid+=1

	for i in range(PLAYER_NUMBER-(0 if AUTO_MODE else 1)):
		p.append(SevensAIPlayer(pid,f"CPU {i+1}",PASS_NUMBER))
		pid+=1

	for i in range(trp.count):
		p[i%PLAYER_NUMBER].addCard(trp.draw())

	for v in p:
		v.sortDeck()

	field=Sevens(p)

	while True:
		field.view()
		for v in p:
			v.selectCard(field)
			if field.checkGameEnd(): break
		else:continue
		break

	field.view()
	field.result()
	input()
