//delexe
using System;
using System.Collections.Generic;
using System.Linq;

static class DEFAULT{
//全自動モード
public const bool AUTO_MODE=false;
//プレイヤー人数
public const int PLAYER_NUMBER=4;
//パス回数
public const int PASS_NUMBER=3;
}

//トランプカードクラス
class TrumpCard{
	public static readonly string[] suitStrs={"▲","▼","◆","■","Jo","JO"};
	public static readonly string[] powerStrs={"Ａ","２","３","４","５","６","７","８","９","10","Ｊ","Ｑ","Ｋ","KR"};
	public readonly string name;
	public readonly int power;
	public readonly int suit;
	public TrumpCard(int suit,int power){
		this.name=TrumpCard.suitStrs[suit]+TrumpCard.powerStrs[power];
		this.power=power;
		this.suit=suit;
	}
}

//トランプの束クラス
class TrumpDeck{
	public static readonly Random rand=new Random();
	const int suits=4;
	const int powers=13;
	readonly IEnumerator<TrumpCard> g;
	List<TrumpCard> deck=new List<TrumpCard>();

	IEnumerator<TrumpCard> trumpIter(List<TrumpCard> deck){
		foreach(var v in deck){
			yield return v;
		}
	}
		
	public int count{get{return deck.Count;}}

	public TrumpDeck(){
		for(var suit=0;suit<suits;suit++){
			for(var power=0;power<powers;power++){
				this.deck.Add(new TrumpCard(suit,power));
			}
		}

		/* Joker
		deck.Add(new TrumpCard(4,powers));
		deck.Add(new TrumpCard(5,powers));
		*/

		g=trumpIter(deck);
	}

	public void shuffle(){
		deck=deck.OrderBy(i=>Guid.NewGuid()).ToList();
	}

	public TrumpCard draw(){
		g.MoveNext();
		return g.Current;
	}
}

//プレイヤークラス
class Player{
	public List<TrumpCard> deck;
	public string name;
	public bool isGameOut;

	public Player(string name){
		deck=new List<TrumpCard>();
		this.name=name;
	}

	public static void sortRefDeck(List<TrumpCard> deck){
		Func<TrumpCard,int> sortValue=v=>v.suit*13+v.power;
		deck=deck.OrderBy(v=>sortValue(v)).ToList();
	}

	public void sortDeck(){Player.sortRefDeck(deck);}

	public void addCard(TrumpCard card){
		deck.Add(card);
	}

	public void removeCard(string cardName){
		deck.Remove(deck.Find(v=>v.name==cardName));
	}

	public int existCard(string cardName){
		return deck.FindIndex(v=>v.name==cardName);
	}

	public void gameOut(){
		isGameOut=true;
	}
}

//カーソル選択モジュール
static class SelectCursors{
	public static int SelectCursor(List<string> items){
		var cursor=0;
		//カーソルの移動
		Action<int,int> move=(x,max)=>{
			cursor+=x;
			if(cursor<0) cursor=0;
			if(max-1<cursor) cursor=max-1;
		};

		//カーソルの表示
		Action view=()=>{
			var select=new bool[items.Count];
			select[cursor]=true;
			var s="";
			for(int i=0;i<items.Count;i++){
				s+=select[i]? $"[{items[i]}]": items[i];
			}
			Console.Write($"{s}\r");
		};

		view();
		for(;;){
			var ch=Console.ReadKey(true);
			if(ch.Key==ConsoleKey.Enter){
				Console.WriteLine();
				return cursor;
			}
			if(ch.Key==ConsoleKey.LeftArrow) move(-1,items.Count);
			if(ch.Key==ConsoleKey.RightArrow) move(1,items.Count);
			view();
		}
	}
}

//七並べプレイヤークラス
class SevensPlayer:Player{
	public int pass;
	public SevensPlayer(string name,int pass):base(name){
		this.pass=pass;
	}

	public virtual void selectCard(Sevens field,int index){
		if(isGameOut) return;
		if(!field.checkPlayNext(this)){
			field.gameOver(this,index);
			field.view();
			Console.WriteLine($"{name} GameOver...\n");
			return;
		}

		Console.WriteLine($"【{name}】Cards: {deck.Count} Pass: {pass}");
		var items=new List<string>(deck.Select(v=>v.name));
		if(0<pass) items.Add("PS:"+pass);

		for(;;){
			var cursor=SelectCursors.SelectCursor(items);

			if(0<pass && items.Count-1==cursor){
				pass--;
				field.view();
				Console.WriteLine($"残りパスは{pass}回です。\n");
				break;
			}
			else if(field.tryUseCard(this,deck[cursor])){
				field.view();
				Console.WriteLine($"俺の切り札!! >「{items[cursor]}」\n");
				if(deck.Count==0){
					Console.WriteLine($"{name} Congratulations!!\n");
					field.gameClear(this,index);
				}
				break;
			}
			else{
				Console.WriteLine("そのカードは出せないのじゃ…\n");
				continue;
			}
		}
	}
}

//七並べAIプレイヤークラス
class SevensAIPlayer:SevensPlayer{
	public SevensAIPlayer(string name,int pass):base(name,pass){}

	public override void selectCard(Sevens field,int index){
		if(isGameOut) return;
		if(!field.checkPlayNext(this)){
			field.gameOver(this,index);
			field.view();
			Console.WriteLine($"{name}> もうだめ...\n");
			return;
		}

		Console.WriteLine($"【{name}】Cards: {deck.Count} Pass: {pass}");
		var items=new List<string>(deck.Select(v=>v.name));
		if(0<pass) items.Add("PS:"+pass);

		Console.Write("考え中...\r");
		System.Threading.Thread.Sleep(1000);

		var passCharge=0;

		for(;;){
			var cursor=TrumpDeck.rand.Next(items.Count);

			if(0<pass && items.Count-1==cursor){
				if(passCharge<3){
					passCharge++;
					continue;
				}
				pass--;
				field.view();
				Console.WriteLine($"パスー (残り{pass}回)\n");
				break;
			}
			else if(field.tryUseCard(this,deck[cursor])){
				field.view();
				Console.WriteLine($"これでも食らいなっ >「{items[cursor]}」\n");
				if(deck.Count==0){
					Console.WriteLine($"{name}> おっさき～\n");
					field.gameClear(this,index);
				}
				break;
			}
			else continue;
		}
	}
}


//トランプの場クラス
class TrumpField{
	public List<TrumpCard> deck=new List<TrumpCard>();

	public void sortDeck(){Player.sortRefDeck(deck);}

	public virtual void useCard(SevensPlayer player,TrumpCard card){
		deck.Add(card);
		player.removeCard(card.name);
	}

	public void view(){
		Console.WriteLine(String.Join(" ",deck.Select(v=>v.name)));
	}
}

//七並べの列クラス
class SevensLine{
	const int jokerIndex=13;
	const int sevenIndex=6;
	public bool[] cardLine=new bool[13];

	public SevensLine(){
		cardLine[sevenIndex]=true;
	}

	public int rangeMin(){
		int i;
		for(i=sevenIndex;0<=i;i--){
			if(!cardLine[i]) return i;
		}
		return i;
	}

	public int rangeMax(){
		int i;
		for(i=sevenIndex;i<jokerIndex;i++){
			if(!cardLine[i]) return i;
		}
		return i;
	}

	public bool checkUseCard(int power){
		if(
			power==jokerIndex ||
			power==rangeMin() ||
			power==rangeMax()
		) return true;
		return false;
	}

	public void useCard(int power){
		cardLine[power]=true;
	}
}

//七並べクラス 
class Sevens:TrumpField{
	const int tenhoh=0xFF;
	SevensLine[] lines;
	int[] rank;
	int clearCount;
	
	public Sevens(List<SevensPlayer> players):base(){
		lines=Enumerable.Range(0,4).Select(x=>new SevensLine()).ToArray();
		
		rank=new int[players.Count];
		clearCount=0;

		for(var i=0;i<4;i++){
			var cardSevenName=TrumpCard.suitStrs[i]+TrumpCard.powerStrs[6];
			for(var n=0;n<players.Count;n++){
				var p=players[n];
				var cardSevenIndex=p.existCard(cardSevenName);
				if(-1<cardSevenIndex){
					var card=p.deck[cardSevenIndex];
					Console.WriteLine($"{p.name} が{card.name}を置きました。");
					useCard(p,card);
					if(p.deck.Count==0){
						Console.WriteLine($"{p.name} 【-- 天和 --】\n");
						rank[n]=tenhoh;
						p.gameOut();
					}
					break;
				}
			}
		}
		Console.WriteLine();
	}

	public override void useCard(SevensPlayer player,TrumpCard card){
		lines[card.suit].useCard(card.power);
		base.useCard(player,card);
	}

	public bool checkUseCard(TrumpCard card){
		return lines[card.suit].checkUseCard(card.power);
	}

	public bool tryUseCard(SevensPlayer player,TrumpCard card){
		if(!checkUseCard(card)) return false;
		useCard(player,card);
		return true;
	}

	public bool checkPlayNext(SevensPlayer player){
		if(0<player.pass) return true;
		foreach(var card in player.deck){
			if(checkUseCard(card)){
				return true;
			}
		}
		return false;
	}

	public void gameClear(SevensPlayer player,int index){
		clearCount++;
		rank[index]=clearCount;
		player.gameOut();
	}

	public void gameOver(SevensPlayer player,int index){
		rank[index]=-1;
		for(var i=player.deck.Count-1;i>=0;i--){
			useCard(player,player.deck[i]);
		}
		player.gameOut();
	}

	public bool checkGameEnd(){
		foreach(var v in rank){
			if(v==0) return false;
		}
		return true;
	}

	public new void view(){
		var s="";
		for(var i=0;i<lines.Length;i++){
			var ss="";
			for(var n=0;n<13;n++){
				if(lines[i].cardLine[n]){
					s+=TrumpCard.suitStrs[i];
					ss+=TrumpCard.powerStrs[n];
				}
				else{
					s+="◇";
					ss+="◇";
				}
			}
			s+="\n"+ss+"\n";
		}
		Console.WriteLine(s);
	}

	public void result(List<SevensPlayer> players){
		Console.WriteLine("\n【Game Result】");
		string rankStr;
		for(var i=0;i<rank.Length;i++){
			if(rank[i]==tenhoh){
				rankStr="天和";
			}
			else if(0<rank[i]){
				rankStr=$"{rank[i]}位";
			}
			else{
				rankStr="GameOver...";
			}
			Console.WriteLine($"{players[i].name}: {rankStr}");
		}
	}
}

//メイン処理
class Program{
	static void Main(){
		for(var i=0;i<100;i++){
			Console.WriteLine();
		}
Console.WriteLine(
@"/---------------------------------------/
/                 七並べ                /
/---------------------------------------/

");

		var trp=new TrumpDeck();
		trp.shuffle();

		var p=new List<SevensPlayer>();
		if(!DEFAULT.AUTO_MODE){
			p.Add(new SevensPlayer("Player",DEFAULT.PASS_NUMBER));
		}

		for(var i=0;i<DEFAULT.PLAYER_NUMBER-(DEFAULT.AUTO_MODE?0:1);i++){
			p.Add(new SevensAIPlayer($"CPU {i+1}",DEFAULT.PASS_NUMBER));
		}

		for(var i=0;i<trp.count;i++){
			p[i%DEFAULT.PLAYER_NUMBER].addCard(trp.draw());
		}

		foreach(var v in p){
			v.sortDeck();
		}

		var field=new Sevens(p);

		for(;;){
			field.view();
			for(var i=0;i<p.Count;i++){
				p[i].selectCard(field,i);
				if(field.checkGameEnd()) goto selectLoop;
			}
		}
		selectLoop:

		field.view();
		field.result(p);
		Console.ReadLine();
	}
}
