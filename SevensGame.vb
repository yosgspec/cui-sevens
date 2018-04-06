'delexe 'only
Option Strict On
Option Infer
Imports System.Collections.Generic
Imports System.Linq

Module _DEFAULT
	'全自動モード
	Public Const AUTO_MODE As Boolean=False
	'プレイヤー人数
	Public Const PLAYER_NUMBER As Integer=4
	'パス回数
	Public Const PASS_NUMBER As Integer=3
End Module

'トランプカードクラス
Class TrumpCard
	Public Shared ReadOnly suitStrs As String()={"▲","▼","◆","■","Jo","JO"}
	Public Shared ReadOnly powerStrs As String()={"Ａ","２","３","４","５","６","７","８","９","10","Ｊ","Ｑ","Ｋ","KR"}
	Public ReadOnly name As String
	Public ReadOnly power As Integer
	Public ReadOnly suit As Integer
	Sub New(suit As Integer,power As Integer)
		Me.name=suitStrs(suit) & powerStrs(power)
		Me.power=power
		Me.suit=suit
	End Sub
End Class

'トランプの束クラス
Class TrumpDeck
	Public Shared ReadOnly rand As New Random()
	Const suits As Integer=4
	Const powers As Integer=13
	ReadOnly g As IEnumerator(Of TrumpCard)
	ReadOnly deck As New List(Of TrumpCard)()

	Private Iterator Function trumpIter(deck As List(Of TrumpCard)) As IEnumerator(Of TrumpCard)
		For Each v In deck
			Yield v
		Next
	End Function

	ReadOnly Property Count() As Integer
		Get
			return deck.Count:End Get:End Property

	Sub New()
		For suit=0I To suits-1
			For power=0I To powers-1
				deck.Add(New TrumpCard(suit,power))
			Next
		Next

		' Joker
		'deck.Add(New TrumpCard(4,powers))
		'deck.Add(New TrumpCard(5,powers))

		g=trumpIter(deck)
	End Sub

	Sub shuffle()
		For i=0i To deck.Count-2
			Dim r=rand.Next(i,deck.Count)
			Dim tmp=deck(i)
			deck(i)=deck(r)
			deck(r)=tmp
		Next
	End Sub

	Function draw() As TrumpCard
		g.MoveNext()
		Return g.Current
	End Function
End Class

'プレイヤークラス
Class Player
	Public ReadOnly deck As New List(Of TrumpCard)()
	public  name As String
	Public isGameOut As Boolean

	Sub New(name As String)
		Me.name=name
	End Sub

	Shared Sub sortRefDeck(deck As List(Of TrumpCard))
		Dim sortValue As Func(Of TrumpCard,Integer)=Function(v) v.suit*13+v.power
		deck.Sort(Function(a,b) sortValue(a)-sortValue(b))
	End Sub
	
	Sub sortDeck
		Player.sortRefDeck(deck):End Sub

	Sub addCard(card As TrumpCard)
		deck.Add(card)
	End Sub

	Sub removeCard(cardName As String)
		deck.Remove(deck.Find(Function(v) v.name=cardName))
	End Sub

	Function existCard(cardName As String) As Integer
		Return deck.FindIndex(Function(v) v.name=cardName)
	End Function

	Sub gameOut()
		isGameOut=True
	End Sub
End Class

'カーソル選択モジュール
Module SelectCursors
	Function SelectCursor(items As List(Of String)) As Integer
		Dim cursor=0I
		'カーソルの移動
		Dim move As Action(Of Integer,Integer)=Sub(x,max)
			cursor+=x
			If  cursor<0 Then cursor=0
			If  max-1<cursor Then cursor=max-1
		End Sub

		'カーソルの表示
		Dim view As Action=Sub()
			Dim _select(items.Count-1) As Boolean
			_select(cursor)=True
			Dim s=""
			For  i=0I To items.Count-1
				s+=If (_select(i),$"[{items(i)}]",items(i))
			Next
			Console.Write($"{s}{vbCr}")
		End Sub

		view()
		Do
			Dim ch=Console.ReadKey(True)
			If  ch.Key=ConsoleKey.Enter Then
				Console.WriteLine()
				Return cursor
			End If 
			If  ch.Key=ConsoleKey.LeftArrow Then move(-1,items.Count)	'左
			If  ch.Key=ConsoleKey.RightArrow Then move(1,items.Count)	'右
			view()
		Loop
	End Function
End Module

'七並べプレイヤークラス
Class SevensPlayer
	Inherits Player
	Public pass As Integer
	Sub New(name As String,pass As Integer)
		MyBase.New(name)
		Me.pass=pass
	End Sub

	OverRidable Sub selectCard(field As Sevens,index As Integer)
		If isGameOut Then Exit Sub
		If Not field.checkPlayNext(Me) Then
			field.gameOver(Me,index)
			field.view()
			Console.WriteLine($"{name} GameOver...{vbLf}")
			Exit Sub
		End If

		Console.WriteLine($"【{name}】Cards: {deck.Count} Pass: {pass}")
		Dim items=New List(Of string)(deck.Select(Function(v) v.name))
		If 0<pass Then items.Add("PS:" & pass)

		Do
			Dim cursor=SelectCursors.SelectCursor(items)

			If 0<pass And items.Count-1=cursor Then
				pass-=1
				field.view()
				Console.WriteLine($"残りパスは{pass}回です。{vbLf}")
				Exit Do
			ElseIf field.tryUseCard(Me,deck(cursor)) Then
				field.view()
				Console.WriteLine($"俺の切り札!! >「{items(cursor)}」{vbLf}")
				If deck.Count=0 Then
					Console.WriteLine($"{name} Congratulations!!{vbLf}")
					field.gameClear(Me,index)
				End If
				Exit Do
			Else
				Console.WriteLine($"そのカードは出せないのじゃ…{vbLf}")
				Continue Do
			End If
		Loop
	End Sub
End Class

'七並べAIプレイヤークラス
Class SevensAIPlayer
	Inherits SevensPlayer
	Sub New(name As String,pass As Integer)
		MyBase.New(name,pass)
	End Sub

	Overrides Sub selectCard(field As Sevens,index As Integer)
		If isGameOut Then Exit Sub
		If Not field.checkPlayNext(Me) Then
			field.gameOver(Me,index)
			field.view()
			Console.WriteLine($"{name}> もうだめ...{vbLf}")
			Exit Sub
		End If

		Console.WriteLine($"【{name}】Cards: {deck.Count} Pass: {pass}")
		Dim items=new List(Of string)(deck.Select(Function(v) v.name))
		If 0<pass Then items.Add("PS:" & pass)

		Console.Write($"考え中...{vbCr}")
		Threading.Thread.Sleep(1000)

		Dim passCharge=0I

		Do
			Dim cursor=TrumpDeck.rand.Next(items.Count)

			If 0<pass And items.Count-1=cursor Then
				If passCharge<3 Then
					passCharge+=1
					Continue Do
				End If

				pass-=1
				field.view()
				Console.WriteLine($"パスー (残り{pass}回){vbLf}")
				Exit Do

			ElseIf field.tryUseCard(Me,deck(cursor)) Then
				Console.WriteLine($"これでも食らいなっ >「{items(cursor)}」{vbLf}")
				If deck.Count=0 Then
					Console.WriteLine($"{name}> おっさき～{vbLf}")
					field.gameClear(Me,index)
				End If
				Exit Do
			Else
				Continue Do
			End If
		Loop
	End Sub
End Class

'トランプの場クラス
Class TrumpField
	ReadOnly deck As New List(Of TrumpCard)()

	Sub sortDeck()
		Player.sortRefDeck(deck):End Sub

	OverRidable Sub useCard(player As SevensPlayer,card As TrumpCard)
		deck.Add(card)
		player.removeCard(card.name)
	End Sub

	OverRidable Sub view()
		Console.WriteLine(String.Join(" ",deck.Select(Function(v) v.name)))
	End Sub
End Class

'七並べの列クラス
Class SevensLine
	Const jokerIndex=13I
	Const sevenIndex=6I
	Public ReadOnly cardLine(13-1) As Boolean

	Sub New()
		cardLine(sevenIndex)=True
	End Sub

	Function rangeMin() As Integer
		Dim i As Integer
		For i=sevenIndex To 0 Step -1
			If Not cardLine(i) Then Return i
		Next
		Return i
	End Function

	Function rangeMax() As Integer
		Dim i As Integer
		For i=sevenIndex To jokerIndex-1
			If Not cardLine(i) Then Return i
		Next
		Return i
	End Function

	Function checkUseCard(power As Integer) As Boolean
		Select Case power
			Case jokerIndex,rangeMin(),rangeMax()
				Return True
			Case Else
				Return False
		End Select
	End Function

	Sub useCard(power As Integer)
		cardLine(power)=True
	End Sub
End Class

'七並べクラス 
Class Sevens
	Inherits TrumpField
	Const tenhoh=&HFFI
	ReadOnly lines As SevensLine()
	ReadOnly rank As Integer()
	Public clearCount As Integer

	Sub New(players As List(Of SevensPlayer))
		MyBase.New()
		lines=Enumerable.Range(0,4).Select(Function(x) New SevensLine()).ToArray()
		rank=New Integer(players.Count-1){}
		clearCount=0

		For i=0I To 3
			Dim cardSevenName=TrumpCard.suitStrs(i) & TrumpCard.powerStrs(6)
			For n=0I To players.Count-1
				Dim p=players(n)
				Dim cardSevenIndex=p.existCard(cardSevenName)
				If -1<cardSevenIndex Then
					Dim card=p.deck(cardSevenIndex)
					Console.WriteLine($"{p.name} が{card.name}を置きました。")
					useCard(p,card)
					If p.deck.Count=0 Then
						Console.WriteLine($"{p.name} 【-- 天和 --】{vbLf}")
						rank(n)=tenhoh
						p.gameOut()
					End If
					Exit For
				End If
			Next
		Next
		Console.WriteLine()
	End Sub

	Overrides Sub useCard(player As SevensPlayer,card As TrumpCard)
		lines(card.suit).useCard(card.power)
		MyBase.useCard(player,card)
	End Sub

	Function checkUseCard(card As TrumpCard) As Boolean
		Return lines(card.suit).checkUseCard(card.power)
	End Function

	Function tryUseCard(player As SevensPlayer,card As TrumpCard) As Boolean
		If Not checkUseCard(card) Then Return False
		useCard(player,card)
		Return True
	End Function

	Function checkPlayNext(player As SevensPlayer) As Boolean
		If 0<player.pass  Then Return True
		For Each card In player.deck
			If checkUseCard(card) Then _
				Return True
		Next
		Return False
	End Function

	Sub gameClear(player As SevensPlayer,index As Integer)
		clearCount+=1
		rank(index)=clearCount
		player.gameOut()
	End Sub

	Sub gameOver(player As SevensPlayer,index As Integer)
		rank(index)=-1
		For i=player.deck.Count-1 To 0 Step -1
			useCard(player,player.deck(i))
		Next
		player.gameOut()
	End Sub

	Function checkGameEnd() As Boolean
		For Each v In rank
			If v=0 Then Return False
		Next
		Return True
	End Function

	OverRides Sub view()
		Dim s=""
		For i=0 To lines.Length-1
			Dim ss=""
			For n=0 To 12
				If lines(i).cardLine(n) Then
					s &=TrumpCard.suitStrs(i)
					ss &=TrumpCard.powerStrs(n)
				Else
					s &="◇"
					ss &="◇"
				End If
			Next
			s &=vbLf & ss & vbLf
		Next
		Console.WriteLine(s)
	End Sub

	Sub result(players As List(Of SevensPlayer))
		Console.WriteLine($"{vbLf}【Game Result】")
		Dim rankStr As String
		For i=0i To rank.Length-1
			If rank(i)=tenhoh Then
				rankStr="天和"
			ElseIf 0<rank(i) Then
				rankStr=$"{rank(i)}位"
			Else
				rankStr="GameOver..."
			End If
			Console.WriteLine($"{players(i).name}: {rankStr}")
		Next
	End Sub
End Class

'メイン処理
Module Program
	Sub Main()
		For i=1I To 100
			Console.WriteLine()
		Next

Console.WriteLine(
$"/---------------------------------------/
/                 七並べ                /
/---------------------------------------/

")
		Dim trp=New TrumpDeck()
		trp.shuffle()

		Dim p=new List(Of SevensPlayer)()

		If Not _DEFAULT.AUTO_MODE Then
			p.Add(New SevensPlayer("Player",_DEFAULT.PASS_NUMBER))
		End If

		For i=1I To _DEFAULT.PLAYER_NUMBER-(If(_DEFAULT.AUTO_MODE,0,1))
			p.Add(New SevensAIPlayer($"CPU {i}",_DEFAULT.PASS_NUMBER))
		Next

		For i=0I To trp.count-1
			p(i Mod _DEFAULT.PLAYER_NUMBER).addCard(trp.draw())
		Next

		For Each v In p
			v.sortDeck()
		Next

		Dim field=New Sevens(p)

		Do
			field.view()
			For i=0i To p.Count-1
				p(i).selectCard(field,i)
				If field.checkGameEnd() Then Exit Do
			Next
		Loop

		field.view()
		field.result(p)
		Console.ReadLine()
	End Sub
End Module
