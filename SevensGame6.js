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
	constructor(suit,power){
		this.name=TrumpCard.suitStrs[suit]+TrumpCard.powerStrs[power];
		this.power=power;
		this.suit=suit
	}
}
TrumpCard.suitStrs=["▲","▼","◆","■","Jo","JO"];
TrumpCard.powerStrs=["Ａ","２","３","４","５","６","７","８","９","10","Ｊ","Ｑ","Ｋ","KR"];
TrumpCard.suits=4;
TrumpCard.powers=13;
Object.freeze(TrumpCard);

//トランプの束クラス
const TrumpDeck=(()=>{
	const g=Symbol();
	const deck=Symbol();

	const trumpIter=function*(deck){
		for(var v of deck){
			yield v;
		}
	}

	return class{
		get count(){return this[deck].length;}

		constructor(jokers=0){
			this[deck]=[];
			for(var suit=0;suit<TrumpCard.suits;suit=0|suit+1){
				for(var power=0;power<TrumpCard.powers;power=0|power+1){
					this[deck].push(new TrumpCard(suit,power));
				}
			}

			while(0<jokers){
				jokers=0|jokers-1;
				this.deck.push(new TrumpCard(TrumpCard.suits+jokers,TrumpCard.powers));
			}

			this[g]=trumpIter(this[deck]);
		}

		shuffle(){
			for(var i=0,imax=this[deck].length-1;i<imax;i=0|i+1){
				var r=0|i+Math.floor(Math.random()*this[deck].length-i);
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
Object.freeze(TrumpDeck);

//プレイヤークラス
class Player{
	constructor(id,name){
		this.deck=[];
		this.id=id;
		this.name=name;
		this.isGameOut=false;
	}

	sortDeck(){
		const sortValue=v=>v.suit*TrumpCard.powers+v.power;
		this.deck.sort((a,b)=>sortValue(a)-sortValue(b));
	}

	addCard(card){
		this.deck.push(card);
	}

	removeCard(cardName){
		this.deck.splice(this.deck.findIndex(v=>v.name==cardName),1);
	}

	existCard(cardName){
		return this.deck.findIndex(v=>v.name==cardName);
	}

	gameOut(){
		this.isGameOut=true;
	}
}
Object.freeze(Player);

//トランプの場クラス
const TrumpField=(()=>{
	const players=Symbol();
	return class{
		constructor(_players){
			this.deck=[];
			if(_players!==undefined) this[players]=_players;
		}

		useCard(player,card){
			this.deck.push(card);
			player.removeCard(card.name);
		}

		view(){
			console.log(this.deck.map(v=>v.name).join(" "));
		}
	};
})();
TrumpField.prototype.sortDeck=Player.prototype.sortDeck;
Object.freeze(TrumpField);

//七並べの列クラス
const SevensLine=(()=>{
	const sevenIndex=6;

	return class{
		constructor(){
			this.cardLine=Array(TrumpCard.powers).fill(false);
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
			for(i=sevenIndex;i<TrumpCard.powers;i=0|i+1){
				if(!this.cardLine[i]) return i;
			}
			return i;
		}

		checkUseCard(power){
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

		useCard(power){
			this.cardLine[power]=true;
		}
	};
})();
Object.freeze(SevensLine);

//七並べクラス 
const Sevens=(()=>{
	const tenhoh=0xFF;
	const players=Symbol();
	const rank=Symbol();

	return class extends TrumpField{
		constructor(_players){
			super();
			this[players]=_players;
			this.lines=Array(TrumpCard.suits).fill({}).map(v=>new SevensLine());
			this[rank]=Array(this[players].length).fill(0);
			this.clearCount=0;

			for(var i=0;i<TrumpCard.suits;i=i=0|i+1){
				var cardSevenName=TrumpCard.suitStrs[i]+TrumpCard.powerStrs[6];
				for(var n in this[players]){
					var p=this[players][n];
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
			this.lines[card.suit].useCard(card.power);
			super.useCard(player,card);
		}

		checkUseCard(card){
			return this.lines[card.suit].checkUseCard(card.power);
		}

		tryUseCard(player,card){
			if(!this.checkUseCard(card)) return false;
			this.useCard(player,card);
			return true;
		}

		checkPlayNext(player,passes){
			if(0<passes) return true;
			for(var card of player.deck){
				if(this.checkUseCard(card)){
					return true;
				}
			}
			return false;
		}

		gameClear(player){
			this.clearCount=0|this.clearCount+1;
			this[rank][player.id]=this.clearCount;
			player.gameOut();
		}

		gameOver(player){
			this[rank][player.id]=-1;
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

		result(){
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
				console.log(`${this[players][i].name}: ${rankStr}`);
			}
		}
	};
})();
Object.freeze(Sevens);

//カーソル選択関数
const SelectCursor=items=>{
	var cursor=0;
	//カーソルの移動
	function move(x,max){
		cursor=0|cursor+x;
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
const SevensPlayer=(()=>{
	const passes=Symbol();
	return class extends Player{
		constructor(id,name,_passes){
			super(id,name);
			if(_passes!==undefined) this[passes]=_passes;
		}

		selectCard(field){return new Promise(resolve=>{
		const g=function*(){
			if(this.isGameOut) return;
			if(!field.checkPlayNext(this,this[passes])){
				field.gameOver(this);
				field.view();
				console.log(`${this.name} GameOver...\n`);
				return;
			}

			console.log(`【${this.name}】Cards: ${this.deck.length} Pass: ${this[passes]}`);
			var items=this.deck.map(v=>v.name);
			if(0<this[passes]) items.push("PS:"+this[passes]);

			for(;;){
				var cursor=yield SelectCursor(items).then(i=>g.next(i));;

				if(0<this[passes] && items.length-1==cursor){
					this[passes]=0|this[passes]-1;
					field.view();
					console.log(`残りパスは${this[passes]}回です。\n`);
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
			resolve();
		}.call(this);
		g.next();
		});}
	};
})();
Object.freeze(SevensPlayer);

//七並べAIプレイヤークラス
const SevensAIPlayer=(()=>{
	const passes=Symbol();
	return class extends SevensPlayer{
		constructor(id,name,_passes){
			super(id,name);
			this[passes]=_passes;
		}

		selectCard(field){return new Promise(resolve=>{
		const g=function*(){
			if(this.isGameOut) return;
			if(!field.checkPlayNext(this,this[passes])){
				field.gameOver(this);
				field.view();
				console.log(`${this.name}> もうだめ...\n`);
				return;
			}

			console.log(`【${this.name}】Cards: ${this.deck.length} Pass: ${this[passes]}`);
			var items=this.deck.map(v=>v.name);
			if(0<this[passes]) items.push("PS:"+this[passes]);

			process.stdout.write("考え中...\r");
			yield setTimeout(()=>g.next(),1000);

			var passCharge=0;

			for(;;){
				var cursor=Math.floor(Math.random()*items.length);

				if(0<this[passes] && items.length-1==cursor){
					if(passCharge<3){
						passCharge=0|passCharge+1;
						continue;
					}
					this[passes]=0|this[passes]-1;
					console.log(`パスー (残り${this[passes]}回)\n`);
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
			resolve();
		}.call(this);
		g.next();
		});}
	};
})();
Object.freeze(SevensAIPlayer);

//メイン処理
const g=function*(){
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
	const p=[];
	var pid=0;

	if(!AUTO_MODE){
		rl.setPrompt("NAME[Player]: ");
		rl.prompt();
		var playerName=yield rl.once("line",s=>g.next(s));
		if(playerName=="") playerName="Player";

		p.push(new SevensPlayer(pid,playerName,PASSES_NUMBER));
		pid=0|pid+1;
	}

	for(var i=0,imax=PLAYER_NUMBER-(AUTO_MODE?0:1);i<imax;i=0|i+1){
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
			yield v.selectCard(field).then(()=>g.next());
			if(field.checkGameEnd()) break selectLoop;
		}
	}

	field.view();
	field.result();
	process.stdin.setRawMode(false);
	rl.once("line",process.exit);
}();
g.next();
