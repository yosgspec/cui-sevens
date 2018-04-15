using System;
using System.Collections.Generic;
using System.Linq;
using static MyGlobal;

static class MyGlobal{
	//全自動モード
	public const bool AUTO_MODE=false;
	//プレイヤー人数
	public const int PLAYER_NUMBER=4;
	//パス回数
	public const int PASSES_NUMBER=3;
}

//トランプカードクラス
class TrumpCard{
	public static readonly string[] suitStrs={"▲","▼","◆","■","Jo","JO"};
	public static readonly string[] powerStrs={"Ａ","２","３","４","５","６","７","８","９","10","Ｊ","Ｑ","Ｋ","KR"};
	public const int suits=4;
	public const int powers=13;
	public readonly string name;
	public readonly int power;
	public readonly int suit;
	public TrumpCard(int suit,int power){
		this.name=suitStrs[suit]+powerStrs[power];
		this.power=power;
		this.suit=suit;
	}
}

//トランプの束クラス
class TrumpDeck{
	public static readonly Random rand=new Random();
	readonly IEnumerator<TrumpCard> g;
	readonly List<TrumpCard> deck=new List<TrumpCard>();

	IEnumerator<TrumpCard> trumpIter(List<TrumpCard> deck){
		foreach(var v in deck){
			yield return v;
		}
	}
		
	public int count{get{return deck.Count;}}

	public TrumpDeck(){
		for(var suit=0;suit<TrumpCard.suits;suit++){
			for(var power=0;power<TrumpCard.powers;power++){
				deck.Add(new TrumpCard(suit,power));
			}
		}

		/* Joker
		deck.Add(new TrumpCard(4,TrumpCard.powers));
		deck.Add(new TrumpCard(5,TrumpCard.powers));
		*/

		g=trumpIter(deck);
	}

	public void shuffle(){
		for(var i=0;i<deck.Count-1;i++){
			var r=rand.Next(i,deck.Count);
			var tmp=deck[i];
			deck[i]=deck[r];
			deck[r]=tmp;
		}
	}

	public TrumpCard draw(){
		g.MoveNext();
		return g.Current;
	}
}

//プレイヤークラス
class Player{
	public readonly List<TrumpCard> deck=new List<TrumpCard>();
	public int id;
	public string name;
	public bool isGameOut;

	public Player(int id,string name){
		this.id=id;
		this.name=name;
	}

	public static void sortRefDeck(List<TrumpCard> deck){
		Func<TrumpCard,int> sortValue=v=>v.suit*TrumpCard.powers+v.power;
		deck.Sort((a,b)=>sortValue(a)-sortValue(b));
	}

	public void sortDeck(){sortRefDeck(deck);}

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

//トランプの場クラス
class TrumpField{
	protected List<Player> players;
	public readonly List<TrumpCard> deck=new List<TrumpCard>();
	public void sortDeck(){Player.sortRefDeck(deck);}

	public TrumpField(List<Player> players){
		this.players=players;
	}

	public virtual void useCard(Player player,TrumpCard card){
		deck.Add(card);
		player.removeCard(card.name);
	}

	public virtual void view(){
		Console.WriteLine(String.Join(" ",deck.Select(v=>v.name)));
	}
}

//七並べの列クラス
class SevensLine{
	const int sevenIndex=6;
	public readonly bool[] cardLine=new bool[TrumpCard.powers];

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
		for(i=sevenIndex;i<TrumpCard.powers;i++){
			if(!cardLine[i]) return i;
		}
		return i;
	}

	public bool checkUseCard(int power){
		if(
			power==TrumpCard.powers ||
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
	public readonly SevensLine[] lines=Enumerable.Range(0,TrumpCard.suits).Select(x=>new SevensLine()).ToArray();
	readonly int[] rank;
	public int clearCount;
	
	public Sevens(List<Player> players):base(players){
		rank=new int[this.players.Count];
		clearCount=0;

		for(var i=0;i<TrumpCard.suits;i++){
			var cardSevenName=TrumpCard.suitStrs[i]+TrumpCard.powerStrs[6];
			for(var n=0;n<this.players.Count;n++){
				var p=this.players[n];
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

	public override void useCard(Player player,TrumpCard card){
		lines[card.suit].useCard(card.power);
		base.useCard(player,card);
	}

	public bool checkUseCard(TrumpCard card){
		return lines[card.suit].checkUseCard(card.power);
	}

	public bool tryUseCard(Player player,TrumpCard card){
		if(!checkUseCard(card)) return false;
		useCard(player,card);
		return true;
	}

	public bool checkPlayNext(Player player,int passes){
		if(0<passes) return true;
		foreach(var card in player.deck){
			if(checkUseCard(card)){
				return true;
			}
		}
		return false;
	}

	public void gameClear(Player player){
		clearCount++;
		rank[player.id]=clearCount;
		player.gameOut();
	}

	public void gameOver(Player player){
		rank[player.id]=-1;
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

	public override void view(){
		var s="";
		for(var i=0;i<TrumpCard.suits;i++){
			var ss="";
			for(var n=0;n<TrumpCard.powers;n++){
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

	public void result(){
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
			if(ch.Key==ConsoleKey.LeftArrow) move(-1,items.Count);	//左
			if(ch.Key==ConsoleKey.RightArrow) move(1,items.Count);	//右
			view();
		}
	}
}

//七並べプレイヤークラス
class SevensPlayer:Player{
	protected int passes;
	public SevensPlayer(int id,string name,int passes):base(id,name){
		this.passes=passes;
	}

	public virtual void selectCard(Sevens field){
		if(isGameOut) return;
		if(!field.checkPlayNext(this,passes)){
			field.gameOver(this);
			field.view();
			Console.WriteLine($"{name} GameOver...\n");
			return;
		}

		Console.WriteLine($"【{name}】Cards: {deck.Count} Pass: {passes}");
		var items=new List<string>(deck.Select(v=>v.name));
		if(0<passes) items.Add("PS:"+passes);

		for(;;){
			var cursor=SelectCursors.SelectCursor(items);

			if(0<passes && items.Count-1==cursor){
				passes--;
				field.view();
				Console.WriteLine($"残りパスは{passes}回です。\n");
				break;
			}
			else if(field.tryUseCard(this,deck[cursor])){
				field.view();
				Console.WriteLine($"俺の切り札!! >「{items[cursor]}」\n");
				if(deck.Count==0){
					Console.WriteLine($"{name} Congratulations!!\n");
					field.gameClear(this);
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
	public SevensAIPlayer(int id,string name,int passes):base(id,name,passes){}

	public override void selectCard(Sevens field){
		if(isGameOut) return;
		if(!field.checkPlayNext(this,passes)){
			field.gameOver(this);
			field.view();
			Console.WriteLine($"{name}> もうだめ...\n");
			return;
		}

		Console.WriteLine($"【{name}】Cards: {deck.Count} Pass: {passes}");
		var items=new List<string>(deck.Select(v=>v.name));
		if(0<passes) items.Add("PS:"+passes);

		Console.Write("考え中...\r");
		System.Threading.Thread.Sleep(1000);

		var passCharge=0;

		for(;;){
			var cursor=TrumpDeck.rand.Next(items.Count);

			if(0<passes && items.Count-1==cursor){
				if(passCharge<3){
					passCharge++;
					continue;
				}
				passes--;
				Console.WriteLine($"パスー (残り{passes}回)\n");
				break;
			}
			else if(field.tryUseCard(this,deck[cursor])){
				Console.WriteLine($"これでも食らいなっ >「{items[cursor]}」\n");
				if(deck.Count==0){
					Console.WriteLine($"{name}> おっさき～\n");
					field.gameClear(this);
				}
				break;
			}
			else continue;
		}
	}
}

//メイン処理
class Program{
	static void Main(){
		for(var i=0;i<100;i++){
			Console.WriteLine();
		}

		Console.WriteLine(@"
/---------------------------------------/
/                 七並べ                /
/---------------------------------------/

");

		var trp=new TrumpDeck();
		trp.shuffle();

		var p=new List<SevensPlayer>();
		var pid=0;
		if(!AUTO_MODE){
			p.Add(new SevensPlayer(pid,"Player",PASSES_NUMBER));
			pid++;
		}

		for(var i=0;i<PLAYER_NUMBER-(AUTO_MODE?0:1);i++){
			p.Add(new SevensAIPlayer(pid,$"CPU {i+1}",PASSES_NUMBER));
			pid++;
		}

		for(var i=0;i<trp.count;i++){
			p[i%PLAYER_NUMBER].addCard(trp.draw());
		}

		foreach(var v in p){
			v.sortDeck();
		}

		var field=new Sevens(p.Select(v=>(Player)v).ToList());

		for(;;){
			field.view();
			foreach(var v in p){
				v.selectCard(field);
				if(field.checkGameEnd()) goto selectLoop;
			}
		}
		selectLoop:

		field.view();
		field.result();
		Console.ReadLine();
	}
}
