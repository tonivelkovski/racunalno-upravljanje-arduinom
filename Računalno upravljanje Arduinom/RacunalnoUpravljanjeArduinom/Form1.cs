//Svi potrebni imenski prostori koji sadrže sve potrebne klase i podklase za ovaj rad
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;                                        
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Drawing.Drawing2D;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace RacunalnoUpravljanjeArduinom
{
    public partial class Form1 : Form
    {
        //Dohvaćanje naziva serijskih portova
        bool isConnected = false;
        bool isEthernet = false;
        String[] ports;
        SerialPort port;
        //Postavljanje mrežne "utičnice" za Ethernet
        Socket sock;
        //Spremanje IP adrese Arduina
        IPAddress serverAddr;
        IPEndPoint endPoint;

        public Form1()
        {
            //Pozivanje zadanih metoda pri pokretanju Forme
            InitializeComponent();
            disableControls();
            getAvailableComPorts();
            connectionDefaults();

        }

        //Ukoliko je pritisnut gumb1,tj. Potvrdi gumb:
        private void button1_Click(object sender, EventArgs e)
        {
            //Ukoliko je varijabla isConnected točna(bool varijabla),ispis poruke
            if (isConnected)
            {
                MessageBox.Show("Molimo odspojite trenutačnu vezu.");
                return;
            }
            connectionDefaults();
            //Odabir u ComboBox izborniku od serijskog,bluetooth ili ethernet načina povezivanja
            if (comboBox1.Text == "Serijska Veza")
            {
                groupBox5.Visible = true;               //Ukoliko je potvrđen odabir "Serijska Veza", pojavljuje se groupBox5
                getAvailableComPorts();                 //Pozivanje metode za prikaz ponuđenih portova
            }
            else if (comboBox1.Text == "Bluetooth")
            {
                groupBox4.Visible = true;               //Prikaz zadanog groupBox-a koji je bio sakriven
            }
            else if (comboBox1.Text == "Ethernet")
            {
                groupBox9.Visible = true;
            }
        }

        //Metoda za dohvaćanje postojećih portova
        void getAvailableComPorts()
        {
            try                                             //Unutar try bloka se izvršavaju zadane naredbe te se pronalaze greške
            {                                               //Try mora završiti sa catch naredbom
                ports = SerialPort.GetPortNames();          //Dohvaćanje imena porta
                comboBox2.Items.Clear();
                foreach (string port in ports)              //Dodavanje imena pronađenog dostupnog porta u ComboBox
                {

                    comboBox2.Items.Add(port);
                    Console.WriteLine(port);
                    if (ports[0] != null)                   //Postavljanje prvog pronađenog porta kao zadanog
                    {
                        comboBox2.SelectedItem = ports[0];
                    }
                }
            }
            catch (Exception e)                             //Catch naredba može, ali i ne mora sadržavati proizvoljnu poruku o grešci
            {                                            
                MessageBox.Show(e.Message);                 //MessageBox koji ispisuje generiranu poruku u slučaju greške
            }
        }
        //Metoda kojom povezujemo Arduino i C# aplikaciju putem serijske veze
        private void connectToArduino()
        {
            try
            {
                string selectedPort = comboBox2.SelectedItem.ToString();
                port = new SerialPort(selectedPort, 9600, Parity.None, 8, StopBits.One);
                port.Open();                                //Otvaranje porta
                isConnected = true;                         //Postavljanje stanja varijable isConnected u true
                button3.Text = "Odspoji";
                SendCom("#STAR\n");                         //Slanje #STAR naredbe na Arduino
                enableControls();                           //Pozivanje enableControls funkcije
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        //Metoda kojom povezujemo Arduino i C# aplikaciju putem bluetooth veze
        private void connectToArduinoBluetooth()
        {
            try
            {
                port = new SerialPort(textBox4.Text, 9600, Parity.None, 8, StopBits.One);   //Postavljanje bluetooth postavki
                port.Open();            
                isConnected = true;
                if (!port.IsOpen) return;                       //Izlaz ukoliko se ne može povezati s portom
                buttonElipse1.Enabled = false;                  //Onemogućavanje odabira "Odspoji" gumba
                MessageBox.Show("Spojeno", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information); //Ispis poruke "Spojeno"
                enableControls();                               //Pozivanje enableControls fukncije
                SendCom("#STAR/n");                             //Slanje #STAR naredbe na Arduino     
            }
            catch (InvalidOperationException portInUse)         //Ukoliko je port zauzet, ispis proizvoljne poruke
            {
                MessageBox.Show("Druga aplikacija koristi odabrani port.\nMolimo zatvorite drugu aplikaciju.\n" + portInUse);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);                     //Ispis generične poruke
            }
        }
        //Metoda za slanje podataka na Arduino
        void SendCom(String data)
        {
            //Ova metoda dodaje trenutne, tj. odabrane naredbe u textBox te ih istodobno šalje na Arduino
            try
            {
                //Dodavanje naredbi u novi red na početku textBox-a
                textBox5.Text = textBox5.Text.Insert(0, (data + Environment.NewLine));
                if (isEthernet)
                {
                    //Slanje naredbi preko ethernet veze
                    byte[] send_buffer = Encoding.ASCII.GetBytes(data);
                    sock.SendTo(send_buffer, endPoint);
                }
                else
                {
                    //Slanje naredbi preko serijske ili bluetooth veze
                    port.Write(data);
                }
            }
            catch 
            {
                   //Try uvijek mora završiti sa catch naredbom, no catch naredba ne mora uvijek sadržavati poruke
            }
        }

        //Odabir prve LED diode            
        private void Led1CheckboxClicked(object sender, EventArgs e)
        {
            if (isConnected)                          //Ukoliko je aplikacija povezana s Arduinom, 
            {
                if (checkBox1.Checked)                //Ukoliko je LED1 odabrana,
                {
                    SendCom("#L11\n");                //Upali LED1 diodu
                }
                else
                {
                    SendCom("#L10\n");                //Ugasi LED1 diodu
                }
            }
        }
        //Odabir druge LED diode; postupak je isti kao i kod prve led diode, jedino se mijenjaju nazivi naredbi te alata
        private void Led2CheckboxClicked(object sender, EventArgs e)
        {
            if (isConnected)
            {
                if (checkBox2.Checked)
                {
                    SendCom("#L21\n");
                }
                else
                {
                    SendCom("#L20\n");
                }
            }
        }
        //Odabir treće LED diode; postupak je isti kao i kod prve led diode, jedino se mijenjaju nazivi naredbi te alata
        private void Led3CheckboxClicked(object sender, EventArgs e)
        {
            if (isConnected)
            {
                if (checkBox3.Checked)
                {
                    SendCom("#L31\n");
                }
                else
                {
                    SendCom("#L30\n");
                }
            }
         }
        //Odabir četvrte LED diode; postupak je isti kao i kod prve led diode, jedino se mijenjaju nazivi naredbi te alata
        private void Led4CheckboxClicked(object sender, EventArgs e)
        {
            if (isConnected)
            {
                if (checkBox4.Checked)
                {
                    SendCom("#L41\n");
                }
                else
                {
                    SendCom("#L40\n");
                }
            }
        }
        //Odabir pete LED diode; postupak je isti kao i kod prve led diode, jedino se mijenjaju nazivi naredbi te alata
        private void Led5CheckboxClicked(object sender, EventArgs e)
        {
            if (isConnected)
            {
                if (checkBox5.Checked)
                {
                    SendCom("#L51\n");
                }
                else
                {
                    SendCom("#L50\n");
                }
            }
        }
        //Metoda za odspajanje aplikacije od Arduina, ukoliko smo povezani serijskom vezom
        private void disconnectFromArduino()
        {
            isConnected = false;                          //Postavljanje stanja isConnected varijable u false
            SendCom("#STOP\n");                           //Slanje naredbe #STOP na Arduino
            port.Close();                                 //Zatvaranje porta
            button3.Text = "Spoji";                       
            disableControls();                            //Pozivanje disableControls funkcije
            resetDefaults();                              //Pozivanje resetDefaults funkcije
        }

        //Metoda za odspajanje aplikacije od Arduina, ukoliko smo povezani preko Bluetootha
        //Sadrži identične naredbe kao i metoda za odspajanje ukoliko smo povezani serijskom vezom
        private void disconnectFromArduinoBluetooth()
        {
            isConnected = false;
            SendCom("#STOP\n");
            port.Close();
            if (port.IsOpen) port.Close();              //Ukoliko je port dostupan, zatvoriti ga,tj. onemogućiti
            buttonElipse1.Enabled = true;               //Omogućiti gumb za povezivanje preko bluetooth veze
            MessageBox.Show("Odspojeno", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
            disableControls();
            resetDefaults();

        }
        //Gumb "Upiši", kojim šaljemo naredbu #TEXT na Arduino za ispis teksta na LCD
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (isConnected)
                {
                    SendCom("#TEXT" + textBox1.Text + "#\n");
                }
            }
            catch
            {
            }
        }
        //Metoda za postavljanje svih potrebnih alata u true, tj. da se svi alati unutar metode omoguće pri pozivanju iste
        private void enableControls()
        {
            checkBox1.Enabled = true;
            checkBox2.Enabled = true;
            checkBox3.Enabled = true;
            checkBox4.Enabled = true;
            checkBox5.Enabled = true;
            button2.Enabled = true;
            button4.Enabled = true;
            textBox1.Enabled = true;
            groupBox1.Enabled = true;
            groupBox3.Enabled = true;
            groupBox6.Enabled = true;
            groupBox7.Enabled = true;
            groupBox8.Enabled = true;

        }
        //Metoda za postavljanje svih potrebnih alata u false, tj. da se svi alati unutar metode onemoguće pri pozivanju iste
        private void disableControls()
        {
            checkBox1.Enabled = false;
            checkBox2.Enabled = false;
            checkBox3.Enabled = false;
            checkBox4.Enabled = false;
            checkBox5.Enabled = false;
            button2.Enabled = false;
            button4.Enabled = false;
            textBox1.Enabled = false;
            groupBox1.Enabled = false;
            groupBox3.Enabled = false;
            groupBox6.Enabled = false;
            groupBox7.Enabled = false;
            groupBox8.Enabled = false;
        }
        //Metoda za postavljanje svih alata koji se nalaze unutar metode na zadane,tj. da resetira zadane alate
        private void resetDefaults()
        {
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            checkBox3.Checked = false;
            checkBox4.Checked = false;
            checkBox5.Checked = false;
            checkBox6.Checked = false;
            checkBox7.Checked = false;
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox5.Text = "";
            trackBar1.Value = 1;
            trackBar2.Value = 200;

        }
        //Metoda za postavljanje zadanih postavki pri povezivanju s Arduinom
        private void connectionDefaults()
        {
            //Sakrivanje groupbox-a pri pozivanju metode
            groupBox4.Visible = false;
            groupBox5.Visible = false;
            groupBox9.Visible = false;
        }
        //Metoda za odabir gumba za bluetooth povezivanje s Arduinom
        private void buttonElipse1_Click(object sender, EventArgs e)
        {
            //Provjera je li bluetooth odabran kao način povezivanja
            if (comboBox1.Text == "Bluetooth")
            {
                buttonElipse1.Enabled = false;              //Onemogućavanje gumba za spajanje s Arduinom
                connectToArduinoBluetooth();                //Pozivanje metode za povezivanje s Arduinom preko bluetooth-a
                buttonElipse2.Enabled = true;               //Omogućavanje gumba za odspajanje od Arduina
                if (!port.IsOpen)
                {
                    //Ukoliko ne uspije povezivanje s odabranim portom, omogućavanje gumba za spajanje te onemogućavanje gumba
                    //za odspajanje od Arduina
                    buttonElipse1.Enabled = true;
                    buttonElipse2.Enabled = false;
                }
            }
        }
        //Metoda za odabir gumba za odspajanje od Arduina preko bluetooth veze
        private void buttonElipse2_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text == "Bluetooth")
            {
                buttonElipse1.Enabled = true;
                buttonElipse2.Enabled = false;
                disconnectFromArduinoBluetooth();
                if (port.IsOpen) port.Close();
            }
        }
        //Metoda za odabir gumba za spajanje s Arduinom putem serijske veze
        private void button3_Click(object sender, EventArgs e)
        {
            //Ukoliko je stanje isConnected varijable true,tj. povezani smo s Arduinom:
            if (isConnected)
            {
               disconnectFromArduino();                     //Pozivanje metode za odspajanje s Arduinom
            }
            else
            {
                connectToArduino();                         //Pozivanje metode za spajanje s Arduinom         
            }
        }
        //Metoda za odabir gumba za prihvaćanje podataka od Arduina,tj. DHT22 senzora
        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                if (isConnected)
                {
                    //Slanje naredbe #DHT na Arduino preko serijske ili bluetooth veze koja nam vraća vrijednosti koje je DHT22 senzor
                    //očitao te proslijedino na aplikaciju
                    SendCom("#DHT\n");
                    //Ukoliko je povezivnje preko etherneta:
                    if (isEthernet)
                    {
                        try
                        {
                            //Kreiranje polja za spremanje dohvaćenih podataka od DHT22 senzora
                            byte[] buf = new byte[64];
                            sock.Receive(buf);              //Prihvaćanje podataka
                            //Ispis podataka na textbox
                            textBox3.Text = Encoding.ASCII.GetString(buf);
                            textBox3.Text += "%";
                            sock.Receive(buf);
                            textBox2.Text = Encoding.ASCII.GetString(buf);
                            textBox2.Text +=" °C";
                        }
                        catch (Exception err)
                        {
                            MessageBox.Show(err.ToString());
                        }
                    }
                    else
                    {
                        //Ukoliko je povezivanje preko serijske ili bluetooth konekcije, iščitavanje podataka DHT22 senzora
                        port.ReadTimeout = 4000;
                        textBox3.Text = port.ReadLine().ToString() + "%";
                        textBox2.Text = port.ReadLine().ToString() + " °C";
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        //Metoda za odabir paljenja piezzo zujalice te njeno upravljanje klizačem
        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (isConnected)
            {
                if (checkBox6.Checked)
                {
                    String frq = "";                        //Prazan string za prikaz frekvencije
                    frq = trackBar2.Value.ToString();       
                    SendCom("#BUZ" + frq + "\n");           //Slanje trenutne vrijednosti klizača na Arduino
                }
                else
                {
                    SendCom("#BUZO\n");                     //Gašenje piezzo zujalice
                }
            }
        }
        //Metoda za postavljanje null vrijednosti klizača
        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            checkBox6_CheckedChanged(sender, null);
        }
        //Metoda za odabir sviranja glazbene ljestvice na Arduinu
        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox7.Checked)
            {
                SendCom("#BUZS\n");                     //Slanje naredbe na Arduino za pokretanje glazbene ljestvice
            }
        }
        //Metoda za odabir paljenja trčećeg svjetla ulijevo
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (isConnected)
            {
                if (radioButton1.Checked)
                {
                    SendCom("#LRL\n");                 //Slanje naredbe za pokretanje trčećeg svjetla ulijevo na Arduino
                }
            }
        }
        //Metoda za odabir paljenja trčećeg svjetla udesno
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (isConnected)
            {
                if (radioButton2.Checked)
                {
                    SendCom("#LRR\n");                //Slanje naredbe za pokretanje trčećeg svijetla ulijevo na Arduino
                }
            }
        }
        //Metoda za gašenje trčećeg svjetla,tj. svih LED dioda
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (isConnected)
            {
                if (radioButton3.Checked)
                {
                    SendCom("#LRN\n");
                }
            }
        }

        //Metoda za upravljanje svjetlinom odabrane LED diode putem klizača
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //Ukoliko su LED4 ili LED5 diode odabrane, onemogućavanje istih te ispis prikladne poruke
            if(checkBox4.Checked || checkBox5.Checked)
            {
                MessageBox.Show("LED4 i LED5 će biti ugašene zbog toga što nisu spojene na PWM pinove.");
                checkBox4.Checked = false;
                checkBox5.Checked = false;
            }
            //Mijenjanje vrijednosti klizača u troznamenkasti broj zbog toga što je raspon svjetline LED diode od 0 do 255
            String brightness = "";
            if (trackBar1.Value < 10)
            {
                brightness = "00";
                brightness += trackBar1.Value.ToString();
            }
            else if (trackBar1.Value < 100)
            {
                brightness = "0";
                brightness += trackBar1.Value.ToString();
            }
            else
            {
                brightness = trackBar1.Value.ToString();
            }
            //Slanje vrijednosti klizača na Arduino te ujedno i postavljanje svjetline LED diode na trenutnu vrijednost klizača
            SendCom("#DIM" + brightness + "#\n");
        }
        //Metoda za odabir gumba koji "čisti", tj. briše sve vrijednosti napisane u textBox5
        private void button4_Click(object sender, EventArgs e)
        {
            textBox5.Text = "";
        }
        //Metoda koja šalje prikazane naredbe na Arduino pri zatvaranju aplikacije
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isConnected)
            {
                if (isEthernet)
                {
                    try
                    {
                        SendCom("#STOP\n");
                        sock.Close();
                    }
                    catch
                    {
                    }
                }
                else
                {
                    disconnectFromArduino();
                }
            }
        }
        //Onemogućavanje upisa odabranih znakova u textBox1
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Onemogućavanje malih dijakritičkih znakova 
            if (e.KeyChar == 'č' || e.KeyChar == 'ć' || e.KeyChar == 'ž' || e.KeyChar == 'š' || e.KeyChar == 'đ')
            {
                e.Handled = true;
            }
            // Onemogućavanje velikih dijakritičkih znakova 
            if (e.KeyChar == 'Č' || e.KeyChar == 'Ć' || e.KeyChar == 'Ž' || e.KeyChar == 'Š' || e.KeyChar == 'Đ')
            {
                e.Handled = true;
            }
        }

        //Pritiskom na Button, otvara nam se nova Forma, novi dijaloški okvir
        private void button6_Click(object sender, EventArgs e)
        {
            Form f2 = new Form2();
            f2.ShowDialog();
        }
        //Pritiskom na gumb "Poveži", pokrećemo povezivanje preko ethernet veze
        private void button7_Click(object sender, EventArgs e)
        {
            button7.Enabled = false;
            try
            {
                if (!isConnected)               //Ukoliko nismo spojeni:
                {
                    //Kreiranje procesa za pokretanje netsh.exe batch skripte koja nam služi za automatsku promjenu mrežne konfiguracije
                    System.Diagnostics.ProcessStartInfo myProcessInfo = new System.Diagnostics.ProcessStartInfo();
                    //Postavljanje netsh.exe lokacije
                    myProcessInfo.FileName = Environment.ExpandEnvironmentVariables("%SystemRoot%") + @"\System32\netsh.exe";
                    //Postavljanje statične IP adrese te IP adrese Ethernet adaptera
                    myProcessInfo.Arguments = "interface ip set address \"Ethernet\" static 169.254.100.100 255.255.0.0 169.254.100.1"; 
                    myProcessInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    myProcessInfo.Verb = "runas";  //Postavljanje pokretanja procesa kao administrator
                    System.Diagnostics.Process.Start(myProcessInfo);                //Pokretanje netsh.exe procesa

                    //Povezivanje preko etherneta, postavljanje mrežne utičnice
                    sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    //Postavljanje IP adrese Arduina za povezivanje
                    serverAddr = IPAddress.Parse(textBox6.Text);
                    endPoint = new IPEndPoint(serverAddr, 8888);
                    MessageBox.Show("Povezivanje...\nPotrajat će nekoliko sekundi");
                    //Čekanje na inicijalizaciju te povezivanje s Arduinom te slanje poznatih naredbi na Arduino
                    Thread.Sleep(4000);
                    enableControls();
                    button7.Text = "Odspoji";
                    isConnected = true;
                    isEthernet = true;
                    SendCom("#STAR\n");
                }
                else
                {
                    try
                    {
                        SendCom("#STOP\n");
                        sock.Close();
                        //Resetiranje IP adrese računala, ponovno kreiranje netsh.exe procesa
                        System.Diagnostics.ProcessStartInfo myProcessInfo = new System.Diagnostics.ProcessStartInfo();
                        //Učitavanje netsh.exe procesa
                        myProcessInfo.FileName = Environment.ExpandEnvironmentVariables("%SystemRoot%") + @"\System32\netsh.exe";
                        //Postavljanje naredbe za resetiranje ip adrese
                        myProcessInfo.Arguments = "interface ip set address \"Ethernet\" dhcp";
                        myProcessInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        myProcessInfo.Verb = "runas";
                        //Pokretanje netsh.exe procesa
                        System.Diagnostics.Process.Start(myProcessInfo);
                    }
                    catch
                    {
                    }
                    disableControls();
                    resetDefaults();
                    button7.Text = "Spoji";
                    isConnected = false;
                    isEthernet = false;
                }
            }
            catch (Exception err)
            {
               MessageBox.Show(err.Message);
            }
            button7.Enabled = true;
        }
        //Pritiskom na gumb "Obriši", brišemo sav tekst iz okvira za upis teksta za ispis na LCD-u
        private void button8_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            if (isConnected)
            {
                SendCom("#STAR\n");
            }
        }
    }
    //Zasebna klasa za kreiranje posebnog, zaobljenog gumba koji nam koriste kod bluetooth povezivanja
    //ButtonElipse je naslijeđen iz klase Button, tj. poprima njegova svojstva
    class ButtonElipse : Button
    {
        protected override void OnPaint(PaintEventArgs pevent)
        {
            GraphicsPath graphics = new GraphicsPath();                         
            graphics.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);
            this.Region = new System.Drawing.Region(graphics);
            base.OnPaint(pevent);
        }
    }
}