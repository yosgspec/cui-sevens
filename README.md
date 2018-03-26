# cui-sevens
コンソール上で動く七並べプログラム

# モジュール構成
![](https://github.com/yosgspec/cui-sevens/blob/master/TrumpGame.png?raw=true)
```
;トランプカードモジュール
#module TrumpCard name,suit,power
	#modcfunc tcName return str name
	#modcfunc tcPower return int power
	#modcfunc tcSuit return int suit
	#define news@TrumpCard ref TrumpCard card,int _suit,int _power
#global

;トランプの束モジュール
#module TrumpDeck co,i,deck,count,hash
	use TrumpCard
	#modcfunc tdCount return int count
	#define new@TrumpDeck ref TrumpDeck td
	#modfunc tdShuffle
	#modfunc tdNext ref TrumpCard card
#global

;プレイヤーモジュール
#module Player deck,cardCount,pass,isGameOut,name
	use TrumpCard
	#modcfunc plName return str name
	#modcfunc plCardCount return int cardCount
	#modcfunc plPass return int pass
	#modcfunc plIsGameOut return int isGameOut
	#modfunc plRefDeck ref TrumpCard() _deck
	#define news@Player ref Player pl,str _name
	#modfunc local super str _name
	#modfunc plSortDeck
	#modfunc plAddCard TrumpCard card
	#modfunc plRemoveCard str cardName
	#modcfunc plExistCard str cardName,return int existCard
	#modfunc plUsePass
	#modfunc plGameOut
#global

;カーソル選択モジュール
#module @CursorSelect
	#defcfunc CursorSelect str() items,return int cursor
#global

;七並べプレイヤーモジュール
#module SevensPlayer deck,cardCount,pass,isGameOut,name,selectCardFn
	use Sevens
	extend Player
	#define news@SevensAIPlaye ref SevensPlayer spl
	#modfunc splSelectCard Sevens _field,int _index
#global

;七並べAIプレイヤーモジュール
#module SevensAIPlayer deck,cardCount,pass,isGameOut,name,selectCardFn
	extend SevensPlayer
	#define news@SevensAIPlayer ref SevensAIPlayer spl,str _name
	#modfunc override splSelectCard Sevens _field,int _index
#global

;トランプの場モジュール
#module TrumpField deck,cardCount
	use Player
	use TrumpCard
	#define new@TrumpField ref TrumpField field
	#modfunc tfUseCard Player _player,TrumpCard card
	#modfunc tfSortField
	#modfunc tfView
#global

;七並べの列モジュール
#module SevensLine cardLine
	#modfunc slRefCardLine ref int() _cardLine
	#define news@SevensLine ref SevensLine sl
	#modcfunc rangeMin return int mix
	#modcfunc rangeMax return int max
	#modcfunc slCheckUseCard int power,return int checkUseCard
	#modfunc slUseCard int power
#global

;七並べモジュール
#module Sevens field,cardCount,lines,rank,clearCount
	use player
	extend TrumpField
	#define new@Sevens ref Sevens field,array players
	#modcfunc svTryUseCard Player _player,TrumpCard card
	#modcfunc svCheckPlayNext Player _player,return int isPlayGame
	#modfunc svGameClear Player _player,int index
	#modfunc svGameOver Player _player,int index
	#modcfunc svCheckGameEnd return int isGameEnd
	#modfunc svView
	#modfunc svResult Player() players
#global

;メイン処理
#module Program
	#deffunc main
#global
main
```
