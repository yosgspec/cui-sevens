"use strict";
const readline=require("readline");
readline.emitKeypressEvents(process.stdin);
const rl=readline.createInterface({
	input:process.stdin,output:process.stdout,prompt:"",terminal:false
});

//全自動モード
const AUTO_MODE=false;
//プレイヤー人数
const PLAYER_NUMBER=4;
//パス回数
const PASSES_NUMBER=3;

//トランプカードクラス
class TrumpCard{
	static readonly suitStrs=["▲","▼","◆","■","Jo","JO"];
	static readonly powerStrs=["Ａ","２","３","４","５","６","７","８","９","10","Ｊ","Ｑ","Ｋ","KR"];
	static readonly suits=4;
	static readonly powers=13;
	name: string;
	power: number;
	suit: number;
	constructor(suit:number,power:number){
		this.name=TrumpCard.suitStrs[suit]+TrumpCard.powerStrs[power];
		this.power=power;
		this.suit=suit
	}
}

//トランプの束クラス
class TrumpDeck{
	private g: IterableIterator<TrumpCard>;
	private deck: TrumpCard[]=[];

	private *trumpIter(deck:TrumpCard[]):IterableIterator<TrumpCard>{
		for(var v of deck){
			yield v;
		}
	}
	get count():number{return this.deck.length;}

	constructor(jokers:number=0){
		for(var suit=0;suit<TrumpCard.suits;suit=0|suit+1){
			for(var power=0;power<TrumpCard.powers;power=0|power+1){
				this.deck.push(new TrumpCard(suit,power));
			}
		}

		while(0<jokers){
			jokers=0|jokers-1;
			this.deck.push(new TrumpCard(TrumpCard.suits+jokers,TrumpCard.powers));
		}

		this.g=this.trumpIter(this.deck);
	}

	shuffle():void{
		for(var i=0,imax=this.deck.length-1;i<imax;i=0|i+1){
			var r=0|i+Math.floor(Math.random()*this.deck.length-i);
			var tmp=this.deck[i];
			this.deck[i]=this.deck[r];
			this.deck[r]=tmp;
		}
	}

	draw():TrumpCard{
		return this.g.next().value;
	}
}

//プレイヤークラス
class Player{
	deck: TrumpCard[]=[];
	id: number;
	name: string;
	isGameOut=false;
	constructor(id:number,name:string){
		this.id=id;
		this.name=name;
	}

	static sortRefDeck(deck:TrumpCard[]):void{
		const sortValue:((TrumpCard)=>number) =v=>0|v.suit*TrumpCard.powers+v.power;
		deck.sort((a,b)=>0|sortValue(a)-sortValue(b));
	}

	sortDeck():void{Player.sortRefDeck(this.deck);}

	addCard(card:TrumpCard):void{
		this.deck.push(card);
	}

	removeCard(cardName:string):void{
		this.deck.splice(this.deck.findIndex(v=>v.name==cardName),1);
	}

	existCard(cardName:string):number{
		return this.deck.findIndex(v=>v.name==cardName);
	}

	gameOut():void{
		this.isGameOut=true;
	}
}

//トランプの場クラス
class TrumpField{
	protected players:Player[];
	deck: TrumpCard[]=[];
	sortDeck():void{Player.sortRefDeck(this.deck);}

	constructor(players:Player[]){
		this.players=players;
	}

	useCard(player:Player,card:TrumpCard):void{
		this.deck.push(card);
		player.removeCard(card.name);
	}

	view():void{
		console.log(this.deck.map(v=>v.name).join(" "));
	}
}

//七並べの列クラス
class SevensLine{
	private readonly sevenIndex=6;
	cardLine=Array(TrumpCard.powers).fill(false);

	constructor(){
		this.cardLine[this.sevenIndex]=true;
	}

	rangeMin():number{
		var i: number;
		for(i=this.sevenIndex;0<=i;i=0|i-1){
			if(!this.cardLine[i]) return i;
		}
		return i;
	}

	rangeMax():number{
		var i: number;
		for(i=this.sevenIndex;i<TrumpCard.powers;i=0|i+1){
			if(!this.cardLine[i]) return i;
		}
		return i;
	}

	checkUseCard(power:number):boolean{
		switch(power){
			case TrumpCard.powers:
				return true;
			case this.rangeMin():
			case this.rangeMax():
				return true;
			default:
				return false;
		}
	}

	useCard(power:number):void{
		this.cardLine[power]=true;
	}
}

//七並べクラス 
class Sevens extends TrumpField{
	private readonly tenhoh=0xFF;
	private rank: number[];
	lines: SevensLine[]=Array(TrumpCard.suits).fill({}).map(v=>new SevensLine());
	clearCount=0;

	constructor(players:Player[]){
		super(players);
		this.players=players;
		this.rank=Array(this.players.length).fill(0);

		for(var i=0;i<TrumpCard.suits;i=i=0|i+1){
			var cardSevenName=TrumpCard.suitStrs[i]+TrumpCard.powerStrs[6];
			for(var n in this.players){
				var p=this.players[n];
				var cardSevenIndex=p.existCard(cardSevenName);
				if(-1<cardSevenIndex){
					var card=p.deck[cardSevenIndex];
					console.log(`${p.name} が${card.name}を置きました。`);
					this.useCard(p,card);
					if(p.deck.length==0){
						console.log(`${p.name} 【-- 天和 --】\n`);
						this.rank[n]=this.tenhoh;
						p.gameOut();
					}
					break;
				}
			}
		}
		console.log();
	}

	useCard(player:Player,card:TrumpCard):void{
		this.lines[card.suit].useCard(card.power);
		super.useCard(player,card);
	}

	checkUseCard(card:TrumpCard):boolean{
		return this.lines[card.suit].checkUseCard(card.power);
	}

	tryUseCard(player:Player,card:TrumpCard):boolean{
		if(!this.checkUseCard(card)) return false;
		this.useCard(player,card);
		return true;
	}

	checkPlayNext(player:Player,passes:number):boolean{
		if(0<passes) return true;
		for(var card of player.deck){
			if(this.checkUseCard(card)){
				return true;
			}
		}
		return false;
	}

	gameClear(player:Player):void{
		this.clearCount=0|this.clearCount+1;
		this.rank[player.id]=this.clearCount;
		player.gameOut();
	}

	gameOver(player:Player):void{
		this.rank[player.id]=-1;
		for(var i=player.deck.length-1;i>=0;i=0|i-1){
			this.useCard(player,player.deck[i]);
		}
		player.gameOut();
	}

	checkGameEnd():boolean{
		for(var v of this.rank){
			if(v==0) return false;
		}
		return true;
	}

	view():void{
		var s="";
		for(var i=0;i<TrumpCard.suits;i=0|i+1){
			var ss="";
			for(var n=0;n<TrumpCard.powers;n=0|n+1){
				if(this.lines[i].cardLine[n]){
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
		console.log(s);
	}

	result():void{
		console.log("\n【Game Result】");
		var rankStr: string;
		for(var i in this.rank){
			if(this.rank[i]==this.tenhoh){
				rankStr="天和";
			}
			else if(0<this.rank[i]){
				rankStr=`${this.rank[i]}位`;
			}
			else{
				rankStr="GameOver...";
			}
			console.log(`${this.players[i].name}: ${rankStr}`);
		}
	}
}

//カーソル選択関数
function SelectCursor(items):Promise<number>{
	var cursor=0;
	//カーソルの移動
	function move(x:number,max:number):void{
		cursor=0|cursor+x;
		if(cursor<0) cursor=0;
		if(max-1<cursor) cursor=0|max-1;
	}

	//カーソルの表示
	function view():void{
		const select=Array(items.length).fill(false);
		select[cursor]=true;
		var s="";
		for(var i in select){
			s+=select[i]? `[${items[i]}]`: `${items[i]}`;
		}
		process.stdout.write(`${s}\r`);
	}

	return new Promise<number>(resolve=>{
		process.stdin.setRawMode(true);
		view();
		process.stdin.on("keypress",function self(k,ch){
			if(ch.name=="return"){
				console.log();
				process.stdin.removeListener("keypress",self);
				return resolve(cursor);
			}
			if(ch.name=="left") move(-1,items.length);	//左
			if(ch.name=="right") move(1,items.length);	//右
			view();
		});
	});
}

//七並べプレイヤークラス
class SevensPlayer extends Player{
	protected passes: number;
	constructor(id:number,name:string,passes:number){
		super(id,name);
		this.passes=passes;
	}

	async selectCard(field:Sevens):Promise<void>{
		if(this.isGameOut) return;
		if(!field.checkPlayNext(this,this.passes)){
			field.gameOver(this);
			field.view();
			console.log(`${this.name} GameOver...\n`);
			return;
		}

		console.log(`【${this.name}】Cards: ${this.deck.length} Pass: ${this.passes}`);
		var items: string[]=this.deck.map(v=>v.name);
		if(0<this.passes) items.push("PS:"+this.passes);

		for(;;){
			var cursor=await SelectCursor(items);

			if(0<this.passes && items.length-1==cursor){
				this.passes=0|this.passes-1;
				field.view();
				console.log(`残りパスは${this.passes}回です。\n`);
				break;
			}
			else if(field.tryUseCard(this,this.deck[cursor])){
				field.view();
				console.log(`俺の切り札!! >「${items[cursor]}」\n`);
				if(this.deck.length==0){
					console.log(`${this.name} Congratulations!!\n`);
					field.gameClear(this);
				}
				break;
			}
			else{
				console.log("そのカードは出せないのじゃ…\n");
				continue;
			}
		}
	}
}

//七並べAIプレイヤークラス
class SevensAIPlayer extends SevensPlayer{
	constructor(id:number,name:string,passes:number){
		super(id,name,passes);
	}

	async selectCard(field:Sevens):Promise<void>{
		if(this.isGameOut) return;
		if(!field.checkPlayNext(this,this.passes)){
			field.gameOver(this);
			field.view();
			console.log(`${this.name}> もうだめ...\n`);
			return;
		}

		console.log(`【${this.name}】Cards: ${this.deck.length} Pass: ${this.passes}`);
		var items: string[]=this.deck.map(v=>v.name);
		if(0<this.passes) items.push("PS:"+this.passes);

		process.stdout.write("考え中...\r");
		await new Promise(res=>setTimeout(res,1000));

		var passCharge=0;

		for(;;){
			var cursor=Math.floor(Math.random()*items.length);

			if(0<this.passes && items.length-1==cursor){
				if(passCharge<3){
					passCharge=0|passCharge+1;
					continue;
				}
				this.passes=0|this.passes-1;
				console.log(`パスー (残り${this.passes}回)\n`);
				break;
			}
			else if(field.tryUseCard(this,this.deck[cursor])){
				console.log(`これでも食らいなっ >「${items[cursor]}」\n`);
				if(this.deck.length==0){
					console.log(`${this.name}> おっさき～\n`);
					field.gameClear(this);
				}
				break;
			}
			else continue;
		}
	}
}

//メイン処理
(async function(){
	for(var i=0;i<100;i=0|i+1){
		console.log();
	}

	console.log(`
/---------------------------------------/
/                 七並べ                /
/---------------------------------------/

`);
	const trp=new TrumpDeck();
	trp.shuffle();
	const p: SevensPlayer[]=[];
	var pid=0;

	if(!AUTO_MODE){
		rl.setPrompt("NAME[Player]: ");
		rl.prompt();
		var playerName=""+(await new Promise(res=>rl.once("line",res)));
		if(playerName=="") playerName="Player";

		p.push(new SevensPlayer(pid,playerName,PASSES_NUMBER));
		pid=0|pid+1;
	}

	for(var i=0,imax=PLAYER_NUMBER-(AUTO_MODE?0:1);i<imax;i++){
		p.push(new SevensAIPlayer(pid,`CPU ${i+1}`,PASSES_NUMBER));
		pid=0|pid+1;
	}

	for(var i=0,imax=trp.count;i<imax;i=0|i+1){
		p[i%PLAYER_NUMBER].addCard(trp.draw());
	}

	for(var v of p){
		v.sortDeck();
	}

	const field=new Sevens(p);

	selectLoop:for(;;){
		field.view();
		for(var v of p){
			await v.selectCard(field);
			if(field.checkGameEnd()) break selectLoop;
		}
	}

	field.view();
	field.result();
	process.stdin.setRawMode(false);
	rl.once("line",process.exit);
})();
