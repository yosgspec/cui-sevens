#include <iostream>
#include <string>
#include <vector>
#include <stdio.h>
#include <conio.h>
#include <random>
#include <algorithm>
#include <thread>
using namespace std;

//全自動モード
#define AUTO_MODE false
//プレイヤー人数
#define  PLAYER_NUMBER 4
//パス回数
#define PASSES_NUMBER 3

//トランプカードクラス
class TrumpCard{
public:
	static const string suitStrs[];
	static const string powerStrs[];
	static const int suits=4;
	static const int powers=13;
	string name;
	int power;
	int suit;
	TrumpCard(int suit,int power){
		this->name=suitStrs[suit]+powerStrs[power];
		this->power=power;
		this->suit=suit;
	}
};
const string TrumpCard::suitStrs[]={"▲","▼","◆","■","Jo","JO"};
const string TrumpCard::powerStrs[]={"Ａ","２","３","４","５","６","７","８","９","10","Ｊ","Ｑ","Ｋ","KR"};

//トランプの束クラス
class TrumpDeck{
private:
	vector<TrumpCard> deck;
	int g;
public:
	static mt19937 rnd;
	int count(){return deck.size();}

	TrumpDeck(){
		for(int suit=0;suit<TrumpCard::suits;suit++){
			for(int power=0;power<TrumpCard::powers;power++){
				TrumpCard card(suit,power);
				deck.push_back(card);
			}
		}

		/* Joker
		deck.push_back(new TrumpCard(4,TrumpCard.powers));
		deck.push_back(new TrumpCard(5,TrumpCard.powers));
		*/

		g=0;
	}

	void shuffle(){
		std::shuffle(deck.begin(),deck.end(),rnd);
	}

	TrumpCard draw(){
		return deck[g++];
	}
};
mt19937 TrumpDeck::rnd([]{
	random_device sd;
	return sd();
}());

//プレイヤークラス
class Player{
public:
	vector<TrumpCard> deck;
	int id;
	string name;
	bool isGameOut=false;

	Player(int id,string name){
		this->id=id;
		this->name=name;
	}

	static void sortRefDeck(vector<TrumpCard> deck){
		auto sortValue=[](TrumpCard v){return v.suit*TrumpCard::powers+v.power;};
		sort(deck.begin(),deck.end(),[&sortValue](TrumpCard a,TrumpCard b){
			return sortValue(a)-sortValue(b);
		});
	}

	void sortDeck(){sortRefDeck(deck);}

	void addCard(TrumpCard card){
		deck.push_back(card);
	}

	void removeCard(string cardName){
		for(int i=0;i<deck.size();i++){
			if(deck[i].name==cardName){
				deck.erase(deck.begin()+i);
				return;
			}
		}
	}

	int existCard(string cardName){
		int existCard=-1;
		for(int i=0;i<deck.size();i++){
			if(deck[i].name==cardName){
				existCard=i;
				break;
			}
		}
		return existCard;
	}

	void gameOut(){
		isGameOut=true;
	}
};

//トランプの場クラス
class TrumpField{
public:
	vector<TrumpCard> deck;
	vector<Player> players;
	void sortDeck(){Player::sortRefDeck(deck);}

	TrumpField(vector<Player>& players){
		this->players=players;
	}

	virtual void useCard(Player player,TrumpCard card){
		deck.push_back(card);
		player.removeCard(card.name);
	}

	virtual void view(){
		string s="";
		for(auto v:deck){
			s+=v.name;
		}
		cout<<s<<endl;
	}
};

//七並べの列クラス
class SevensLine{
private:
	const int sevenIndex=6;
public:
	vector<bool> cardLine;

	SevensLine(){
		for(int i=0;i<TrumpCard::powers;i++) cardLine.push_back(false);
		cardLine[sevenIndex]=true;
	}

	int rangeMin(){
		int i;
		for(i=sevenIndex;0<=i;i--){
			if(!cardLine[i]) return i;
		}
		return i;
	}

	int rangeMax(){
		int i;
		for(i=sevenIndex;i<TrumpCard::powers;i++){
			if(!cardLine[i]) return i;
		}
		return i;
	}

	bool checkUseCard(int power){
		if(
			power==TrumpCard::powers ||
			power==rangeMin() ||
			power==rangeMax()
		) return true;
		return false;
	}

	void useCard(int power){
		cardLine[power]=true;
	}
};

//七並べクラス 
class Sevens:public TrumpField{
private:
	const int tenhoh=0xFF;
	vector<int> rank;
public:
	vector<SevensLine> lines;
	int clearCount;
	Sevens(vector<Player>& players):TrumpField(players){
		for(int i=0;i<TrumpCard::suits;i++) lines.push_back([]{
			SevensLine v;
			return v;
		}());
		for(int i=0;i<this->players.size();i++) rank.push_back(0);
		clearCount=0;

		for(int i=0;i<TrumpCard::suits;i++){
			auto cardSevenName=TrumpCard::suitStrs[i]+TrumpCard::powerStrs[6];
			for(int n=0;n<this->players.size();n++){
				auto& p=this->players[n];
				auto cardSevenIndex=p.existCard(cardSevenName);
				if(-1<cardSevenIndex){
					auto card=p.deck[cardSevenIndex];
					cout<<p.name<<" が"<<card.name<<"を置きました。"<<endl;
					useCard(p,card);
					if(p.deck.size()==0){
						cout<<p.name<<" 【-- 天和 --】\n"<<endl;
						rank[n]=tenhoh;
						p.gameOut();
					}
					break;
				}
			}
		}
		cout<<endl;
	}

	virtual public void useCard(Player& player,TrumpCard card){
		lines[card.suit].useCard(card.power);
		TrumpField::useCard(player,card);
	}

	virtual public bool checkUseCard(TrumpCard card){
		return lines[card.suit].checkUseCard(card.power);
	}

	virtual public bool tryUseCard(Player& player,TrumpCard card){
		if(!checkUseCard(card)) return false;
		useCard(player,card);
		return true;
	}

	virtual public bool checkPlayNext(Player& player,int passes){
		if(0<passes) return true;
		for(auto card:player.deck){
			if(checkUseCard(card)){
				return true;
			}
		}
		return false;
	}

	virtual public void gameClear(Player& player){
		clearCount++;
		rank[player.id]=clearCount;
		player.gameOut();
	}

	virtual public void gameOver(Player& player){
		rank[player.id]=-1;
		for(auto i=player.deck.size()-1;i>=0;i--){
			useCard(player,player.deck[i]);
		}
		player.gameOut();
	}

	virtual public bool checkGameEnd(){
		for(auto v:rank){
			if(v==0) return false;
		}
		return true;
	}

	virtual void view(){
		string s="";
		for(int i=0;i<TrumpCard::suits;i++){
			string ss="";
			for(int n=0;n<TrumpCard::powers;n++){
				if(lines[i].cardLine[n]){
					s+=TrumpCard::suitStrs[i];
					ss+=TrumpCard::powerStrs[n];
				}
				else{
					s+="◇";
					ss+="◇";
				}
			}
			s+="\n"+ss+"\n";
		}
		cout<<s<<endl;
	}

	virtual void result(){
		cout<<"\n【Game Result】"<<endl;
		string rankStr;
		for(int i=0;i<rank.size();i++){
			if(rank[i]==tenhoh){
				rankStr="天和";
			}
			else if(0<rank[i]){
				rankStr=to_string(rank[i])+"位";
			}
			else{
				rankStr="GameOver...";
			}
			cout<<players[i].name<<": "<<rankStr<<endl;
		}
	}
};

//カーソル選択モジュール
int SelectCursor(vector<string> items){
	auto cursor=0;
	//カーソルの移動
	auto move=[&](int x,int max){
		cursor+=x;
		if(cursor<0) cursor=0;
		if(max-1<cursor) cursor=max-1;
	};

	//カーソルの表示
	auto view=[&]{
		vector<bool> select(items.size(),false);
		select[cursor]=true;
		string s="";
		for(int i=0;i<items.size();i++){
			s+=select[i]? "["+items[i]+"]": items[i];
		}
		cout<<s<<"\r"<<flush;
	};

	view();
	for(;;){
		auto ch=getch();
		if(ch==0x0d){
			cout<<endl;
			break;
		}
		if(ch==0xe0){
			ch=getch();
			if(ch==0x4b) move(-1,items.size());	//左
			if(ch==0x4d) move(1,items.size());	//右
		}
		view();
	}
	return cursor;
}

//七並べプレイヤークラス
class SevensPlayer:public Player{
public:
	int passes;
	SevensPlayer(int id,string name,int passes):Player(id,name){
		this->passes=passes;
	}

	virtual void selectCard(Sevens& field){
		if(isGameOut) return;
		if(!field.checkPlayNext(*this,passes)){
			field.gameOver(*this);
			field.view();
			cout<<name<<" GameOver...\n"<<endl;
			return;
		}

		cout<<"【"<<name<<"】Cards: "<<deck.size()<<"} Pass: "<<passes<<endl;
		vector<string> items;
		for(auto v:deck) items.push_back(v.name);
		if(0<passes) items.push_back("PS:"+to_string(passes));

		for(;;){
			int cursor=SelectCursor(items);

			if(0<passes && items.size()-1==cursor){
				passes--;
				field.view();
				cout<<"残りパスは"<<passes<<"回です。\n"<<endl;
				break;
			}
			else if(field.tryUseCard(*this,deck[cursor])){
				field.view();
				cout<<"俺の切り札!! >「"<<items[cursor]<<"」\n"<<endl;
				if(deck.size()==0){
					cout<<name<<" Congratulations!!\n"<<endl;
					field.gameClear(*this);
				}
				break;
			}
			else{
				cout<<"そのカードは出せないのじゃ…\n"<<endl;
				continue;
			}
		}
	}
};

//七並べAIプレイヤークラス
class SevensAIPlayer:public SevensPlayer{
public:
	SevensAIPlayer(int id,string name,int passes):SevensPlayer(id,name,passes){}
	virtual void selectCard(Sevens& field){
		if(isGameOut) return;
		if(!field.checkPlayNext(*this,passes)){
			field.gameOver(*this);
			field.view();
			cout<<name<<"> もうだめ...\n"<<endl;
			return;
		}

		cout<<"【"<<name<<"】Cards: "<<deck.size()<<"} Pass: "<<passes<<endl;
		vector<string> items;
		for(auto v:deck) items.push_back(v.name);
		if(0<passes) items.push_back("PS:"+to_string(passes));


		cout<<"考え中...\r"<<flush;
		this_thread::sleep_for(chrono::seconds(1));

		int passCharge=0;
		uniform_int_distribution<int> randItem(0,items.size()-1);
		for(;;){
			auto cursor=randItem(TrumpDeck::rnd);

			if(0<passes && items.size()-1==cursor){
				if(passCharge<3){
					passCharge++;
					continue;
				}
				passes--;
				cout<<"パスー (残り"<<passes<<"回)\n"<<endl;
				break;
			}
			else if(field.tryUseCard(*this,deck[cursor])){
				cout<<"これでも食らいなっ >「"<<items[cursor]<<"」\n"<<endl;
				if(deck.size()==0){
					cout<<name<<"> おっさき～\n"<<endl;
					field.gameClear(*this);
				}
				break;
			}
			else continue;
		}
	}
};

//メイン処理
int main(){

	for(auto i=0;i<100;i++){
		cout<<"\n";
	}
cout<<
"/---------------------------------------/\n"<<
"/                 七並べ                /\n"<<
"/---------------------------------------/\n"<<
"\n\n"<<endl;

	TrumpDeck trp;
	trp.shuffle();

	vector<SevensPlayer> p;
	int pid=0;
	if(!AUTO_MODE){
		SevensPlayer player(pid,"Player",PASSES_NUMBER);
		p.push_back(player);
		pid++;
	}

	for(auto i=0;i<PLAYER_NUMBER-(AUTO_MODE?0:1);i++){
		SevensAIPlayer player(pid,"CPU"+to_string(i+1),PASSES_NUMBER);
		p.push_back(player);
		pid++;
	}

	for(int i=0;i<trp.count();i++){
		p[i%PLAYER_NUMBER].addCard(trp.draw());
	}

	for(auto& v:p){
		v.sortDeck();
	}

	Sevens field([&]{
		vector<Player> pp;
		for(auto& v:p){
			Player& vv=v;
			pp.push_back(vv);
		}
		return pp;
	}());
	
	for(;;){
		field.view();
		for(auto& v:p){
			v.selectCard(field);
			if(field.checkGameEnd()) goto selectLoop;
		}
	}
	selectLoop:

	field.view();
	field.result();
	while(getchar()!='\n');
	return 0;
}
