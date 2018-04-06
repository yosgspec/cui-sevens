"use strict";

//全自動モード
const AUTO_MODE=false;
//プレイヤー人数
const PLAYER_NUMBER=4;
//パス回数
const PASS_NUMBER=3;

//トランプカードクラス
class TrumpCard{
	constructor(suit,power){
		this.name=TrumpCard.suitStrs[suit]+TrumpCard.powerStrs[power];
		this.power=power;
		this.suit=suit
	Object.freeze(this);
	}
}
TrumpCard.suitStrs=["▲","▼","◆","■","Jo","JO"];
TrumpCard.powerStrs=["Ａ","２","３","４","５","６","７","８","９","10","Ｊ","Ｑ","Ｋ","KR"];
Object.freeze(TrumpCard);

//トランプの束クラス
const TrumpDeck=(()=>{
	const suits=4;
	const powers=13;
	const g=Symbol();
	const deck=Symbol();

	const trumpIter=function*(deck){
		for(var v of deck){
			yield v;
		}
	}

	return class{
		get count(){return this[deck].length;}

		constructor(){
			this[deck]=[];
			for(var suit=0;suit<suits;suit=0|suit+1){
				for(var power=0;power<powers;power=0|power+1){
					this[deck].push(new TrumpCard(suit,power));
				}
			}

			/* Joker
			this[deck].push(new TrumpCard(4,powers));
			this[deck].push(new TrumpCard(5,powers));
			*/

			this[g]=trumpIter(this[deck]);
		}

		shuffle(){
			for(var i=0,imax=this[deck].length-1;i<imax;i=0|i+1){
				var r=i+Math.floor(Math.random()*this[deck].length-i);
				var tmp=this[deck][i];
				this[deck][i]=this[deck][r];
				this[deck][r]=tmp;
			}
		}

		draw(){
			return this[g].next().value;
		}
	};
})();
Object.freeze(TrumpDeck);;

//プレイヤークラス
class Player{
	constructor(name){
		this.deck=[];
		this.name=name;
		this.isGameOut=false;
	}

	sortDeck(){
		const sortValue=v=>v.suit*13+v.power;
		this.deck.sort((a,b)=>sortValue(a)-sortValue(b));
	}

	addCard(card){
		this.deck.push(card);
	}

	removeCard(cardName){
		this.deck.some((v,i)=>v.name==cardName? this.deck.splice(i,1): false);
	}

	existCard(cardName){
		return this.deck.findIndex(v=>v.name==cardName);
	}

	gameOut(){
		this.isGameOut=true;
	}
}
Object.freeze(Player);

//カーソル選択関数
require("readline").emitKeypressEvents(process.stdin);
const SelectCursor=items=>{
	var cursor=0;
	//カーソルの移動
	function move(x,max){
		cursor+=x;
		if(cursor<0) cursor=0;
		if(max-1<cursor) cursor=max-1;
	}

	//カーソルの表示
	function view(){
		const select=Array(items.length).fill(false);
		select[cursor]=true;
		var s="";
		for(var i in select){
			s+=select[i]? `[${items[i]}]`: `${items[i]}`;
		}
		process.stdout.write(`${s}\r`);
	}

	return new Promise(resolve=>{
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
};
Object.freeze(SelectCursor);

//七並べプレイヤークラス
class SevensPlayer extends Player{
	constructor(name,pass){
		super(name);
		this.pass=pass;
	}

	async selectCard(field,index){
		if(this.isGameOut) return;
		if(!field.checkPlayNext(this)){
			field.gameOver(this,index);
			field.view();
			console.log(`${this.name} GameOver...\n`);
			return;
		}

		console.log(`【${this.name}】Cards: ${this.deck.length} Pass: ${this.pass}`);
		var items=this.deck.map(v=>v.name);
		if(0<this.pass) items.push("PS:"+this.pass);

		for(;;){
			var cursor=await SelectCursor(items);

			if(0<this.pass && items.length-1==cursor){
				this.pass=0|this.pass-1;
				field.view();
				console.log(`残りパスは${this.pass}回です。\n`);
				break;
			}
			else if(field.tryUseCard(this,this.deck[cursor])){
				field.view();
				console.log(`俺の切り札!! >「${items[cursor]}」\n`);
				if(this.deck.length==0){
					console.log(`${this.name} Congratulations!!\n`);
					field.gameClear(this,index);
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
Object.freeze(SevensPlayer);

//七並べAIプレイヤークラス
class SevensAIPlayer extends SevensPlayer{
	constructor(name,pass){
		super(name,pass);
	}

	async selectCard(field,index){
		if(this.isGameOut) return;
		if(!field.checkPlayNext(this)){
			field.gameOver(this,index);
			field.view();
			console.log(`${this.name}> もうだめ...\n`);
			return;
		}

		console.log(`【${this.name}】Cards: ${this.deck.length} Pass: ${this.pass}`);
		var items=this.deck.map(v=>v.name);
		if(0<this.pass) items.push("PS:"+this.pass);

		process.stdout.write("考え中...\r");
		await new Promise(res=>setTimeout(res,1000));

		var passCharge=0;

		for(;;){
			var cursor=Math.floor(Math.random()*items.length);

			if(0<this.pass && items.length-1==cursor){
				if(passCharge<3){
					passCharge=0|passCharge+1;
					continue;
				}
				this.pass=0|this.pass-1;
				field.view();
				console.log(`パスー (残り${this.pass}回)\n`);
				break;
			}
			else if(field.tryUseCard(this,this.deck[cursor])){
				console.log(`これでも食らいなっ >「${items[cursor]}」\n`);
				if(this.deck.length==0){
					console.log(`${this.name}> おっさき～\n`);
					field.gameClear(this,index);
				}
				break;
			}
			else continue;
		}
	}
}
Object.freeze(SevensAIPlayer);

//トランプの場クラス
class TrumpField{
	constructor(){
		this.deck=[];
		this.sortDeck=Player.prototype.sortDeck;
		Object.freeze(this.sortDeck);
	}

	useCard(player,card){
		this.deck.push(card);
		player.removeCard(card.name);
	}

	view(){
		console.log(this.deck.map(v=>v.name).join(" "));
	}
}
Object.freeze(TrumpField);

//七並べの列クラス
const SevensLine=(()=>{
	const jokerIndex=13;
	const sevenIndex=6;

	return class{
		constructor(){
			this.cardLine=Array(13).fill(false);
			this.cardLine[sevenIndex]=true;
		}

		rangeMin(){
			var i;
			for(i=sevenIndex;0<=i;i=0|i-1){
				if(!this.cardLine[i]) return i;
			}
			return i;
		}

		rangeMax(){
			var i;
			for(i=sevenIndex;i<jokerIndex;i=0|i+1){
				if(!this.cardLine[i]) return i;
			}
			return i;
		}

		checkUseCard(power){
			switch(power){
				case jokerIndex:
					return true;
				case this.rangeMin():
				case this.rangeMax():
					return true;
				default:
					return false;
			}
		}

		useCard(power){
			this.cardLine[power]=true;
		}
	}
})();
Object.freeze(SevensLine);

//七並べクラス 
const Sevens=(()=>{
	const tenhoh=0xFF;
	const lines=Symbol();
	const rank=Symbol();
	const clearCount=Symbol();
	
	return class extends TrumpField{
		constructor(players){
			super();
			this[lines]=Array(4).fill({}).map(v=>new SevensLine());
			this[rank]=Array(players.length).fill(false);
			this[clearCount]=0;

			for(var i=0;i<4;i=i=0|i+1){
				var cardSevenName=TrumpCard.suitStrs[i]+TrumpCard.powerStrs[6];
				for(var n in players){
					var p=players[n];
					var cardSevenIndex=p.existCard(cardSevenName);
					if(-1<cardSevenIndex){
						var card=p.deck[cardSevenIndex];
						console.log(`${p.name} が${card.name}を置きました。`);
						this.useCard(p,card);
						if(p.deck.length==0){
							console.log(`${p.name} 【-- 天和 --】\n`);
							this[rank][n]=tenhoh;
							p.gameOut();
						}
						break;
					}
				}
			}
			console.log();
		}

		useCard(player,card){
			this[lines][card.suit].useCard(card.power);
			super.useCard(player,card);
		}

		checkUseCard(card){
			return this[lines][card.suit].checkUseCard(card.power);
		}

		tryUseCard(player,card){
			if(!this.checkUseCard(card)) return false;
			this.useCard(player,card);
			return true;
		}

		checkPlayNext(player){
			if(0<player.pass) return true;
			for(var card of player.deck){
				if(this.checkUseCard(card)){
					return true;
				}
			}
			return false;
		}

		gameClear(player,index){
			this[clearCount]=0|this[clearCount]+1;
			this[rank][index]=this[clearCount];
			player.gameOut();
		}

		gameOver(player,index){
			this[rank][index]=-1;
			for(var i=player.deck.length-1;i>=0;i=0|i-1){
				this.useCard(player,player.deck[i]);
			}
			player.gameOut();
		}

		checkGameEnd(){
			for(var v of this[rank]){
				if(v==0) return false;
			}
			return true;
		}

		view(){
			var s="";
			for(var i in this[lines]){
				var ss="";
				for(var n=0;n<13;n=0|n+1){
					if(this[lines][i].cardLine[n]){
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

		result(players){
			console.log("\n【Game Result】");
			var rankStr;
			for(var i in this[rank]){
				if(rank[i]==tenhoh){
					rankStr="天和";
				}
				else if(0<this[rank][i]){
					rankStr=`${this[rank][i]}位`;
				}
				else{
					rankStr="GameOver...";
				}
				console.log(`${players[i].name}: ${rankStr}`);
			}
		}
	}
})();
Object.freeze(Sevens);

//メイン処理
(async function(){
	for(var i=0;i<100;i=0|i+1){
		console.log();
	}
console.log(
`/---------------------------------------/
/                 七並べ                /
/---------------------------------------/

`);

	const trp=new TrumpDeck();
	trp.shuffle();

	const p=[];
	if(!AUTO_MODE){
		p.push(new SevensPlayer("Player",PASS_NUMBER));
	}

	for(var i=0,imax=PLAYER_NUMBER-(AUTO_MODE?0:1);i<imax;i++){
		p.push(new SevensAIPlayer(`CPU ${i+1}`,PASS_NUMBER));
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
		for(var i in p){
			await p[i].selectCard(field,i);
			if(field.checkGameEnd()) break selectLoop;
		}
	}

	field.view();
	field.result(p);
	process.stdin.setRawMode(true);
	process.stdin.once("data",process.exit);
})();
