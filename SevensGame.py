from typing import List,Iterator,Callable
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

	def __init__(self,suit:int,power:int)->None:
		self.name=TrumpCard.suitStrs[suit]+TrumpCard.powerStrs[power]
		self.power=power
		self.suit=suit

#トランプの束クラス
class TrumpDeck:
	def trumpIter(self,deck:List[TrumpCard]):
		for v in self.deck:
			yield v

	@property
	def count(self)->int:
		return len(self.deck)

	def __init__(self,jokers=0)->None:
		self.deck: List[TrumpCard]=[]
		for suit in range(TrumpCard.suits):
			for power in range(TrumpCard.powers):
				self.deck.append(TrumpCard(suit,power))

		while 0<jokers:
			jokers-=1
			self.deck.append(TrumpCard(TrumpCard.suits+jokers,TrumpCard.powers))

		self.__g: Iterator=self.trumpIter(self.deck)

	def shuffle(self)->None:
		random.shuffle(self.deck)

	def draw(self)->TrumpCard:
		return next(self.__g)

#プレイヤークラス
class Player:
	def __init__(self,id:int,name:str)->None:
		self.deck: List[TrumpCard]=[]
		self.id=id
		self.name=name
		self.isGameOut=False

	@staticmethod
	def sortRefDeck(deck:List[TrumpCard])->None:
		sortValue: Callable[[TrumpCard],int]=lambda v: v.suit*TrumpCard.powers+v.power
		deck.sort(key=sortValue)

	def sortDeck(self)->None: Player.sortRefDeck(self.deck)

	def addCard(self,card:TrumpCard)->None:
		self.deck.append(card)

	def removeCard(self,cardName:str)->None:
		self.deck.pop([v.name for v in self.deck].index(cardName))

	def existCard(self,cardName:str)->int:
		try:
			return [v.name for v in self.deck].index(cardName)
		except ValueError:
			return -1

	def gameOut(self)->None:
		self.isGameOut=True

#トランプの場クラス
class TrumpField:
	def sortDeck(self)->None: Player.sortRefDeck(self.deck)
	def __init__(self,players:List[Player])->None:
		self.deck: List[TrumpCard]=[]
		self._players=players

	def useCard(self,player:Player,card:TrumpCard)->None:
		self.deck.append(card)
		player.removeCard(card.name)

	def view(self)->None:
		print(" ".join([v.name for v in self.deck]))

#七並べの列クラス
class SevensLine:
	__sevenIndex=6

	def __init__(self)->None:
		self.cardLine: List[bool]=[False for i in range(TrumpCard.powers)]
		self.cardLine[SevensLine.__sevenIndex]=True

	def rangeMin(self)->int:
		i: int
		for i in range(SevensLine.__sevenIndex,-1,-1):
			if not self.cardLine[i]: return i
		return i

	def rangeMax(self)->int:
		i: int
		for i in range(SevensLine.__sevenIndex,TrumpCard.powers):
			if not self.cardLine[i]: return i
		return i

	def checkUseCard(self,power:int)->bool:
		if(
			power==TrumpCard.powers or
			power==self.rangeMin() or 
			power==self.rangeMax()
		):
			return True
		else:
			return False

	def useCard(self,power:int)->None:
		self.cardLine[power]=True

#七並べクラス 
class Sevens(TrumpField):
	__tenhoh=0xFF

	def __init__(self,players:List[Player])->None:
		super().__init__(players)
		self.lines: List[SevensLine]=[SevensLine() for i in range(TrumpCard.suits)]
		self.__rank: List[int]=[0 for i in self._players]
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
						self.__rank[n]=self.__tenhoh
						p.gameOut()
					break
		print()

	def useCard(self,player:Player,card:TrumpCard)->None:
		self.lines[card.suit].useCard(card.power)
		super().useCard(player,card)

	def checkUseCard(self,card:TrumpCard)->bool:
		return self.lines[card.suit].checkUseCard(card.power)

	def tryUseCard(self,player:Player,card:TrumpCard)->bool:
		if not self.checkUseCard(card): return False
		self.useCard(player,card)
		return True

	def checkPlayNext(self,player:Player,passes:int)->bool:
		if 0<passes: return True
		for card in player.deck:
			if self.checkUseCard(card):
				return True
		return False

	def gameClear(self,player:Player)->None:
		self.clearCount+=1
		self.__rank[player.id]=self.clearCount
		player.gameOut()

	def gameOver(self,player:Player)->None:
		self.__rank[player.id]=-1
		for i in range(len(player.deck)-1,-1,-1):
			self.useCard(player,player.deck[i])
		player.gameOut()

	def checkGameEnd(self)->bool:
		for v in self.__rank:
			if v==0: return False
		return True

	def view(self)->None:
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

	def result(self)->None:
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
def SelectCursor(items:List[str])->int:
	cursor=0
	#カーソルの移動
	def move(x:int,max:int)->None:
		nonlocal cursor
		cursor+=x
		if cursor<0: cursor=0
		if max-1<cursor: cursor=max-1

	#カーソルの表示
	def view()->None:
		nonlocal items,cursor
		select: List[bool]=[False for i in items]
		select[cursor]=True
		s=""
		for i in range(len(items)):
			s+=f"[{items[i]}]" if select[i] else items[i]
		print(f"{s}\r",end="")

	view()
	try:
		from msvcrt import getch
		keyCursor=0xe0
		keyLeft=0x4b
		keyRight=0x4d
		getchLinux=False
	except ImportError:
		def getch():
			import sys
			import tty
			import termios
			fd=sys.stdin.fileno()
			old=termios.tcgetattr(fd)
			try:
				tty.setraw(fd)
				return sys.stdin.read(1)
			finally:
				termios.tcsetattr(fd,termios.TCSADRAIN,old)
		keyCursor=0x1b
		keyLeft=0x44
		keyRight=0x43
		getchLinux=True

	while True:
		ch=ord(getch())
		if ch==0x0d:
			print()
			break

		if ch==keyCursor:
			if getchLinux: getch()
			ch=ord(getch())
			if ch==keyLeft: move(-1,len(items))	#左
			if ch==keyRight: move(1,len(items))	#右
			
		view()
	return cursor

#七並べプレイヤークラス
class SevensPlayer(Player):
	def __init__(self,id:int,name:str,passes:int)->None:
		super().__init__(id,name)
		self._passes=passes

	def selectCard(self,field:Sevens)->None:
		if self.isGameOut: return
		if not field.checkPlayNext(self,self._passes):
			field.gameOver(self)
			field.view()
			print(f"{self.name} GameOver...\n")
			return

		print(f"【{self.name}】Cards: {len(self.deck)} Pass: {self._passes}")
		items: List[str]=[v.name for v in self.deck]
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
	def __init__(self,id:int,name:str,passes:int)->None:
		super().__init__(id,name,passes)

	def selectCard(self,field:Sevens)->None:
		if self.isGameOut: return
		if not field.checkPlayNext(self,self._passes):
			field.gameOver(self)
			field.view()
			print(f"{self.name}> もうだめ...\n")
			return

		print(f"【{self.name}】Cards: {len(self.deck)} Pass: {self._passes}")
		items: List[str]=[v.name for v in self.deck]
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

	print("""
/---------------------------------------/
/                 七並べ                /
/---------------------------------------/

""")
	trp=TrumpDeck()
	trp.shuffle()
	p: List[SevensPlayer]=[]
	pid=0

	if not AUTO_MODE:
		playerName=input("NAME[Player]: ")
		if playerName=="": playerName="Player"

		p.append(SevensPlayer(pid,playerName,PASS_NUMBER))
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
