#include <iostream>
#include <string>
#include <vector>
#include <stdio.h>
#include <conio.h>
#include <random>
#include <algorithm>
#include <thread>
using namespace std;

//�S�������[�h
#define AUTO_MODE false
//�v���C���[�l��
#define  PLAYER_NUMBER 4
//�p�X��
#define PASSES_NUMBER 3

//�g�����v�J�[�h�N���X
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
const string TrumpCard::suitStrs[]={"��","��","��","��","Jo","JO"};
const string TrumpCard::powerStrs[]={"�`","�Q","�R","�S","�T","�U","�V","�W","�X","10","�i","�p","�j","KR"};

//�g�����v�̑��N���X
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

//�v���C���[�N���X
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

//�g�����v�̏�N���X
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

//�����ׂ̗�N���X
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

//�����׃N���X 
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
					cout<<p.name<<" ��"<<card.name<<"��u���܂����B"<<endl;
					useCard(p,card);
					if(p.deck.size()==0){
						cout<<p.name<<" �y-- �V�a --�z\n"<<endl;
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
					s+="��";
					ss+="��";
				}
			}
			s+="\n"+ss+"\n";
		}
		cout<<s<<endl;
	}

	virtual void result(){
		cout<<"\n�yGame Result�z"<<endl;
		string rankStr;
		for(int i=0;i<rank.size();i++){
			if(rank[i]==tenhoh){
				rankStr="�V�a";
			}
			else if(0<rank[i]){
				rankStr=to_string(rank[i])+"��";
			}
			else{
				rankStr="GameOver...";
			}
			cout<<players[i].name<<": "<<rankStr<<endl;
		}
	}
};

//�J�[�\���I�����W���[��
int SelectCursor(vector<string> items){
	auto cursor=0;
	//�J�[�\���̈ړ�
	auto move=[&](int x,int max){
		cursor+=x;
		if(cursor<0) cursor=0;
		if(max-1<cursor) cursor=max-1;
	};

	//�J�[�\���̕\��
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
			if(ch==0x4b) move(-1,items.size());	//��
			if(ch==0x4d) move(1,items.size());	//�E
		}
		view();
	}
	return cursor;
}

//�����׃v���C���[�N���X
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

		cout<<"�y"<<name<<"�zCards: "<<deck.size()<<"} Pass: "<<passes<<endl;
		vector<string> items;
		for(auto v:deck) items.push_back(v.name);
		if(0<passes) items.push_back("PS:"+to_string(passes));

		for(;;){
			int cursor=SelectCursor(items);

			if(0<passes && items.size()-1==cursor){
				passes--;
				field.view();
				cout<<"�c��p�X��"<<passes<<"��ł��B\n"<<endl;
				break;
			}
			else if(field.tryUseCard(*this,deck[cursor])){
				field.view();
				cout<<"���̐؂�D!! >�u"<<items[cursor]<<"�v\n"<<endl;
				if(deck.size()==0){
					cout<<name<<" Congratulations!!\n"<<endl;
					field.gameClear(*this);
				}
				break;
			}
			else{
				cout<<"���̃J�[�h�͏o���Ȃ��̂���c\n"<<endl;
				continue;
			}
		}
	}
};

//������AI�v���C���[�N���X
class SevensAIPlayer:public SevensPlayer{
public:
	SevensAIPlayer(int id,string name,int passes):SevensPlayer(id,name,passes){}
	virtual void selectCard(Sevens& field){
		if(isGameOut) return;
		if(!field.checkPlayNext(*this,passes)){
			field.gameOver(*this);
			field.view();
			cout<<name<<"> ��������...\n"<<endl;
			return;
		}

		cout<<"�y"<<name<<"�zCards: "<<deck.size()<<"} Pass: "<<passes<<endl;
		vector<string> items;
		for(auto v:deck) items.push_back(v.name);
		if(0<passes) items.push_back("PS:"+to_string(passes));


		cout<<"�l����...\r"<<flush;
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
				cout<<"�p�X�[ (�c��"<<passes<<"��)\n"<<endl;
				break;
			}
			else if(field.tryUseCard(*this,deck[cursor])){
				cout<<"����ł��H�炢�Ȃ� >�u"<<items[cursor]<<"�v\n"<<endl;
				if(deck.size()==0){
					cout<<name<<"> ���������`\n"<<endl;
					field.gameClear(*this);
				}
				break;
			}
			else continue;
		}
	}
};

//���C������
int main(){

	for(auto i=0;i<100;i++){
		cout<<"\n";
	}
cout<<
"/---------------------------------------/\n"<<
"/                 ������                /\n"<<
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
