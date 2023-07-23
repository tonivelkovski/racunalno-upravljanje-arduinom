//Biblioteke potrebne za rad odabranih elemenata 
#include <LiquidCrystal_I2C.h>        //Biblioteka za LCD display
#include <Wire.h>                     //Biblioteka za LCD display
#include <SoftwareSerial.h>           //Biblioteka za HC-05 Bluetooth modul
#include <SPI.h>                      //Biblioteka za SPI komunikaciju, potrebna za starije verzije Arduina
#include <Ethernet.h>                 //Biblioteka za Ethernet komunikaciju
#include <EthernetUdp.h>              //Biblioteka za Ethernet komunikaciju
#include "DHT.h"                      //Biblioteka potrebna za DHT22 senzor
                              
//Inicijalizacija LED dioda i postavki potrebnih za njihovo upravljanje
int led[] = {3, 5, 6, 7, 8};          //Inicijalizacija prikazanih pinova za LED diode
int dim = 255;                        //Inicijalizacija varijable "dim" sa postavljenom vrijednošću '255'
int rn = 0;                           
boolean state[] = {0, 0, 0, 0, 0};    

//Inicijalizacija Piezzo zujalice i potrebnih postavki
#define PIEZO_PIN 2
#define NUM_OF_TONES 10
int tones[] = {261, 277, 294, 311, 330, 349, 370, 392, 415, 440};

//Inicijalizacija LCD display-a
LiquidCrystal_I2C lcd(0x27, 16, 2);

//Inicijalizacija DHT11 senzora
#define DHTTYPE DHT11 
#define DHTPIN A1                     //Definiranje pina za DHT11 senzor
DHT dht(DHTPIN, DHTTYPE);
char buffr[5];                        //Varijabla koja pohranjuje DHT vrijednosti

//Inicijalizacija HC-05 Bluetooth modula
SoftwareSerial BTSerial(A2, A3);

//Inicijalizacija Ethernet shielda,tj. potrebnih stvari za Ethernet komunikaciju
byte mac[] = {
  0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED  //Postavljanje MAC adrese
};
IPAddress ip(169, 254, 100, 101);     //IP adresa Arduina
IPAddress remote_ip(0, 0, 0, 0);      //Spremnik IP adrese računala pri primitku podataka
unsigned int remote_port = 80;        
unsigned int localPort = 8888;        //Lokalni port 
char packetBuffer[64];                //Međuspremnici za primanje i slanje podataka
EthernetUDP Udp;    
                
//Inicijalizacija varijabli potrebnih za povezivanje računala i aplikacije
bool stringComplete = false;          //String kompletan ili nije
String inputString = "";              //String koji zadržava dolazne podatke
String commandString = "";
bool isConnected = false;

//Setup kod
void setup() {
  Serial.begin(9600);                 //Inicijalizacija serijske komunikacije
  BTSerial.begin(9600);               //Inicijalizacija Bluetooth serijske komunikacije
  Ethernet.begin(mac, ip);            //Pokretanje ethernet shield-a i postavljanje IP adrese
  Udp.begin(localPort);
  pinMode(PIEZO_PIN, OUTPUT);         //Postavljanje Piezzo pin-a kao izlazni
  pinMode(13, OUTPUT);                
  for (int i = 0; i < 5; i++) {
    pinMode(led[i], OUTPUT);          //Postavljanje svih pinova LED dioda kao izlazni
  }
  initDisplay();                      //Pokretanje zadane funkcije
  dht.begin();                        //Pokretanje DHT22 senzora
}

//Petlja koja se beskonačno ponavlja
void loop() {               
  //Ovisno o odabiru, pozivanje funkcije za trčeće svjetlo u odabranom smjeru
  if (rn == 1) {
    turnRunningLeft();
  }
  else if (rn == 2) {
    turnRunningRight();
  }

  //Pozivanje triju fukncija za triju različitih načina povezivanja
  serialEvent();                     
  bluetoothEvent();                  
  ethernetEvent();  
  
  //Ako je string kompletan,
  if (stringComplete) {
    stringComplete = false;
    getCommand();
    if (commandString.equals("STAR")){
      lcd.clear();                    //Ako je primljeni string "STAR", brisanje teksta s LCD-a 
    }
  
  //Ako je primljeni string "STOP":
  else if (commandString.equals("STOP"))
  {
    for (int i = 0; i < 5; i++) {
      turnLedOff(led[i]);             //Gašenje svih LED dioda
      }
      noTone(PIEZO_PIN);              //Gašenje piezzo zujalice
      rn = 0;
      turnRunningOf();
      lcd.clear();                    //Brisanje zaslona 
      lcd.setCursor(2,0);             //Postavljanje pozicije na LCD-u te ispis teksta
      lcd.print("Spreman za");        //Ispis odgovarajućeg teksta na LCD zaslon
      lcd.setCursor(2,1);
      lcd.print("povezivanje.");
  }
  
  //Prikaz dobivenog teksta na LCD
  else if (commandString.equals("TEXT")) {                 //Ako je dobivena naredba jednaka TEXT,
      printText(getTextToPrint());                         //pozivanje funkcije za ispis na LCD
  }
  
  //Upravljanje LED diodama
  else if (commandString.substring(0, 3) == "L11") {       //Ako je dobiveni podatak jednak L11,
    turnLedOn(led[0]);                                     //poziva se funkcija koja pali prvu LED diodu,
    state[0] = 1;                                          //te se u boolean polje upiše '1' 
  }
  else if (commandString.substring(0, 3) == "L10") {       //Ako je dobiveni podatak jednak L10,
    turnLedOff(led[0]);                                    //poziva se funkcija koja gasi prvu LED diodu,
    state[0] = 0;                                          //te se u boolean polje upiše '0'
  }
  else if (commandString.substring(0, 3) == "L21") {       
    turnLedOn(led[1]);
    state[1] = 1;
  }
  else if (commandString.substring(0, 3) == "L20") {
    turnLedOff(led[1]);
    state[1] = 0;
  }
  else if (commandString.substring(0, 3) == "L31") {
    turnLedOn(led[2]);                                    //Naredbe se ponavljaju te se jedino izmjenjuju
    state[2] = 1;                                         //dobiveni podatci,tj. s kojim LED diodama upravljamo
  }
  else if (commandString.substring(0, 3) == "L30") {
    turnLedOff(led[2]);
    state[2] = 0;
  }
  else if (commandString.substring(0, 3) == "L41") {
    turnLedOn(led[3]);
    state[3] = 1;
  }
  else if (commandString.substring(0, 3) == "L40") {
    turnLedOff(led[3]);
    state[3] = 0;
  }
  else if (commandString.substring(0, 3) == "L51") {
    turnLedOn(led[4]);
    state[4] = 1;
  }
  else if (commandString.substring(0, 3) == "L50") {
    turnLedOff(led[4]);
    state[4] = 0;
  }
  //Trčeće svjetlo
  else if (commandString.substring(0, 3) == "LRL") {      //Kao i kod LED dioda,ako je dobiveni podatak jednak
    rn = 1;                                               //"LRL",varijablu rn postavljamo u '1' te ovisno o
  }                                                       //odabiru na početku koda,palimo trčeće svjetlo
  else if (commandString.substring(0, 3) == "LRR") {
    rn = 2;
  }
  else if (commandString.substring(0, 3) == "LRN") {
    rn = 0;                                               //Zaustavljanje trčećeg svjetla
    turnRunningOf();
  }

  //DHT22 očitavanje podataka
  else if (commandString.substring(0, 3) == "DHT") {     //Ukoliko je primljeni podatak jednak "DHT",
    float h = dht.readHumidity();                        //očitava se vlažnost i temperatura
    float t = dht.readTemperature();
    Serial.println(h);                                   //Slanje podataka preko serijske ili Bluetooh veze:
    Serial.println(t);
    BTSerial.println(h);
    BTSerial.println(t);
    //Slanje vrijednosti vlage preko Ethernet veze:
    float_to_String(h);                         //Funkcija koja pretvara float tip u string tip podatak
    Udp.beginPacket(remote_ip, remote_port);
    Udp.write(buffr);                           
    Udp.endPacket();
    //Slanje vrijednosti temperature preko Ethernet veze:
    float_to_String(t);
    Udp.beginPacket(remote_ip, remote_port);
    Udp.write(buffr);
    Udp.endPacket();
  }

  //Regulacija svjetline LED dioda
  else if (commandString.substring(0, 3) == "DIM") {         //Ukoliko je primljeni podatak jednak "DIM",
    const char num_1 = inputString[6];                       //Pretvorba string podataka u char oblik podataka
    const char num_10 = inputString[5];
    const char num_100 = inputString[4];
    int dim_x = (num_1 - '1');                             //Pretvorba string podataka u int oblik podataka:
    dim_x += ((num_10 - '1') + 1) * 10;
    dim_x += ((num_100 - '1') + 1) * 100;
    dim = dim_x;
    //Ispis zadane "dim" vrijednosti,tj. vrijednosti svjetline odabranih LED dioda
    for (int i = 0; i < 3; i++){
      analogWrite(led[i], state[i] * dim);
    }
    digitalWrite(led[3], LOW);                         //Gašenje četvrte i pete LED diode zbog toga što pinovi
    digitalWrite(led[4], LOW);                         //na koje su spojene ne podržavaju PWM
  }

  //Upravljanje piezzo zujalicom
  else if (commandString.substring(0, 3) == "BUZ") {  //Ukoliko je primljeni podatak jednak "BUZ",
    if (commandString[3] == 'S') {                    //Ukoliko je nastavak "S", pozivanje funkcije za
      piezzoScale();                                  //sviranje glazbene ljestvice
    }
    else if (commandString[3] == 'O') {
      noTone(PIEZO_PIN);
    }
    else {
      const char num_1 = inputString[6];            //Pretvaranje vrijednosti tipa string u tip int 
      const char num_10 = inputString[5];           
      const char num_100 = inputString[4];
      int dim_x = (num_1 - '1');
      dim_x += ((num_10 - '1') + 1) * 10;
      dim_x += ((num_100 - '1') + 1) * 100;
      tone(PIEZO_PIN, dim_x);
    }
  }
  inputString = "";   //Brisanje dolaznog stringa kako bismo mogli primiti novi
  }
}


//Postavljanje početnog zapisa na LCD display
void initDisplay(){
  lcd.begin();
  lcd.setCursor(2,0);             //Postavljanje pozicije na LCD-u te ispis teksta
  lcd.print("Spreman za");
  lcd.setCursor(2,1);
  lcd.print("povezivanje.");
}

//Postavljanje fukncije za prihvaćanje naredbi
void getCommand(){
  if (inputString.length() == 0) return;              
  commandString = inputString.substring(1, 5);
}

//Funkcije za paljenje i gašenje LED dioda
void turnLedOn(int pin){
  digitalWrite(pin, HIGH);       //Paljenje
}

void turnLedOff(int pin){
  digitalWrite(pin, LOW);        //Gašenje
}

//Funkcije za pokretanje i zaustavljanje trčećeg svjetla
void turnRunningRight(){            //Trčeće svjetlo u desno
  for (int i = 4; i >= 0; i--){
    digitalWrite(led[i], HIGH);
    delay(200);
    digitalWrite(led[i], LOW);
  }
}
void turnRunningLeft(){             //Trčeće svjetlo u lijevo
  for (int i = 0; i < 5; i++){
    digitalWrite(led[i], HIGH);
    delay(200);
    digitalWrite(led[i], LOW);
  }
}
void turnRunningOf(){              //Funkcija koja zaustavlja trčeće svjetlo,tj. gasi sve LED diode
  for (int i = 0; i < 5; ++i) turnLedOff(led[i]);
}

//Funkcija kojom prihvaćamo upisani tekst
String getTextToPrint(){
  String out = inputString;
  return out.substring(5, inputString.length() - 2);
}

//Funkcija kojom upisujemo dobiveni tekst na LCD display
void printText(const String& text){
  lcd.clear();
  lcd.setCursor(0, 0);
  if (text.length() < 16){                //Funkcija ispisuje tekst na LCD display po redovima,prvo 16 mogućih
    lcd.print(text);                      //znakova u prvom redu,zatim 16 mogućih znakova u drugom redu
    return;
  }
  lcd.print(text.substring(0, 16));
  lcd.setCursor(0, 1);
  lcd.print(text.substring(16, 32));
}

//Funkcija koja pokreće sviranje glazbene ljestvice na piezzo zujalici
void piezzoScale() {
  for (int i = 0; i < NUM_OF_TONES; i++){     //Na odabrani pin za piezzo zujalicu,sviranje trenutne na redu
    tone(PIEZO_PIN, tones[i]);                //frekvencije iz prethodno definiranog polja tones[] sa 
    delay(500);                               //odgodom od pola sekunde (500ms)
  }
  noTone(PIEZO_PIN);
}

//Funkcija za serijsko povezivanje
void serialEvent() {
  while (Serial.available()) {
    const char inChar = (char)Serial.read();      //Prihvaćanje novog bajta
    inputString += inChar;                        //Postavljanje istog u inputString varijablu
    if (inChar == '\n') {                         //Ukoliko je dolazni znak novi red, postavljanje vrijednosti
      stringComplete = true;                      //stringComplete u "true"
    }
  }
}

//Funkcija za Bluetooth povezivanje
void bluetoothEvent() {                           //Iste naredbe kao i za serijsko povezivanje, samo što se 
  while (BTSerial.available()) {                  //ovdje serijska komunikacija odvija putem HC-05 Bluetooth
    const char inChar = (char)BTSerial.read();    //modula
    inputString += inChar;
    if (inChar == '\n') {
      stringComplete = true;
    }
  }
}

//Funkcija za povezivanje putem Etherneta
void ethernetEvent() {
  int packetSize = Udp.parsePacket();
  if (packetSize){                                
    remote_ip = Udp.remoteIP();                   //Dohvaćanje remote IP adrese
    remote_port = Udp.remotePort();            
    Udp.read(packetBuffer, 64);                   //Iščitavanje paketa u packetBuffer 
    inputString = packetBuffer;                   //Spremanje podataka u inputString varijablu
    Serial.println(packetBuffer);
    stringComplete = true;
  }
  delay(10);
}

//Funkcija za pretvaranje float tipa u char tip
void float_to_String(float f) {
  f *= 100;                                     //Pretvaranja float tipa u int (npr. 12,34 --> 1234)
  int n = f;
  buffr[0] = (n / 1000) + 48;                   //Dohvaćanje druge decimalne znamenke i njenog ASCII koda
  n %= 1000;                                    //npr. 12,34 --> dohvaćena znamenka: 4, ASCII kod: 52
  buffr[1] = (n / 100) + 48;                    //Dohvaćanje prve decimalne znamenke i njenog ASCII koda
  n %= 100;
  buffr[2] = '.';
  buffr[3] = (n / 10) + 48;
  n %= 10;
  buffr[4] = (n / 1) + 48;
}
