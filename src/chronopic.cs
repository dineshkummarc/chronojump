/*
 * Copyright (C) 2005  Juan Gonzalez Gomez
 * Copyright (C) 2014-2015  Xavier de Blas <xaviblas@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 */

using System;
using System.IO.Ports;
using System.Threading;
//using System.Runtime.InteropServices;

using System.Diagnostics; 	//for detect OS
using System.IO; 		//for detect OS
using Mono.Unix;

public class Chronopic {

	//****************************
	//  TIPOS PUBLICOS
	//****************************
	public enum ErrorType
	{
		Ok = 0,        //-- OK. No hay error
		Timeout = 1,   //-- Error por Timeout
		Invalid = 2,   //-- Error por recibir caracter invalido
	}

	//-- Estado de la plataforma
	public enum Plataforma : int
	{
		ON = 1,
		OFF = 0,
		UNKNOW = -1,
	}

	//******************************
	//  CONSTRUCTORES Y DESTRUCTORES
	//******************************

	//-- Fake Constructor
	//only used when there's a problem in detection and must return a Chronopic instance
	//see gui/chronopic.cs chronopicInit
	public Chronopic() {
	}

	//-- Constructor
	public Chronopic(SerialPort sp)
	{
		//-- Comprobar si puerto serie ya estaba abierto
		if (sp != null)
			if (sp.IsOpen)
				sp.Close();

		//-- Abrir puerto serie
		sp.Open();

		//-- Configurar timeout por defecto
		//de momento no en windows (hasta que no encontremos por qué falla)
		//OperatingSystem os = Environment.OSVersion;
		//not used, now there's no .NET this was .NET related
		//on mono timeouts work on windows and linux
		//if( ! os.Platform.ToString().ToUpper().StartsWith("WIN"))
			sp.ReadTimeout = DefaultTimeout;

		//-- Guardar el puerto serie
		this.sp = sp;

//		//-- Vaciar buffer
//		//done in a separate method
//		this.flush();
	}

	//-- Destructor
	~Chronopic()
	{
		//-- Cerrar puerto serie
		if (sp != null)
			if (sp.IsOpen)
				sp.Close();
	}

	//***************************************
	//  METODOS PUBLICOS
	//***************************************

	//--------------------------------------------------
	//-- Leer un evento en Chronopic
	//-- Devuelve:
	//--   * timestamp : Marca de tiempo
	//--   * plataforma: Nuevo estado de la plataforma
	//--------------------------------------------------
	public bool Read_event(out double timestamp, 
			out Plataforma plataforma)
	{
		double t;

		//-- Trama de Evento
		byte[] trama = new byte[5];
		bool ok;

		//-- Esperar a que llegue la trama o que se
		//-- produzca un timeout
		ok = Read_cambio(trama);

		//-- Si hay timeout o errores
		if (ok==false) {
			plataforma = Plataforma.UNKNOW;
			timestamp = 0.0;



			return false;
		}

		//-- Comprobar que el estado transmitido en la trama es correcto
		//-- El estado de la plataforma solo puede tener los valores 0,1
		if (trama[1]!=0 && trama[1]!=1) {
			//-- Trama erronea
			plataforma = Plataforma.UNKNOW;
			timestamp = 0.0;
			return false;
		}

		//-- Actualizar el estado
		if (trama[1]==0)
			plataforma = Plataforma.OFF;
		else 
			plataforma = Plataforma.ON;

		//-- Obtener el tiempo
		t = (double)((trama[2]*65536 + trama[3]*256 + trama[4])*8)/1000;

		timestamp = t;

		return true;
	}   
	
	public void Read_variables_automatic()
	{
		LogB.Information("ReadVarAutoStart");

		if (sp == null)
			sp.Open(); 
		
		LogB.Information("ReadVarAutoOpened");

		try {
			sp.Write("J");
			LogB.Information("Port scanning (should return 'J'): " + (char) sp.ReadByte());
		} catch {
			this.error=ErrorType.Timeout;
			LogB.Information("Timeout. This is not Chronopic-Automatic-Firmware");
			return;
		}

		
		sp.Write("V");
		LogB.Information("Version: " + 
				(char) sp.ReadByte() +
				(char) sp.ReadByte() +
				(char) sp.ReadByte() 
			       	);

		int debounce = 0;

		sp.Write("a");
		debounce = ( sp.ReadByte() - '0' ) * 10;
		LogB.Information("Current debounce time:", debounce.ToString());

		LogB.Information("Changing to 10 ms ... ");
		sp.Write("b\x01");

		sp.Write("a");
		debounce = ( sp.ReadByte() - '0' ) * 10;
		LogB.Information("Current debounce time:", debounce.ToString());

		LogB.Information("Changing to 50 ms ... ");
		sp.Write("b\x05");

		sp.Write("a");
		debounce = ( sp.ReadByte() - '0' ) * 10;
		LogB.Information("Current debounce time: ", debounce.ToString());
	}



	//----------------------------------------
	//-- Obtener el estado de la plataforma
	//----------------------------------------
	public bool Read_platform(out Plataforma plataforma)
	{
		//-- Crear la trama
		byte[] trama = {(byte)Trama.Estado};
		byte[] respuesta = new byte[2];
		int n;
		int count;
		bool status;
			
		if (sp != null)
			if (sp.IsOpen) 
				sp.Close();
		
		try {
			sp.Open();
		} catch {
			status=false;
			plataforma = Plataforma.UNKNOW;
			this.error=ErrorType.Timeout;
			return status;
		}

		//-- Enviar la trama por el puerto serie
		sp.Write(trama,0,1);

		//-- Esperar a que llegue la respuesta
		//-- Se espera hasta que en el buffer se tengan el numero de bytes
		//-- esperados para la trama. (para esta trama 2). Si hay un 
		//-- timeout se aborta
		count=0;
		do {
			n = sp.Read(respuesta,count,2-count);
			count+=n;
		} while (count<2 && n!=-1);

		//-- Comprobar la respuesta recibida
		switch(count) {
			case 2 : //-- Datos listos
				if (respuesta[0]==(byte)Trama.REstado) {
					switch (respuesta[1]) {
						case 0: 
							plataforma = Plataforma.OFF;
							this.error=ErrorType.Ok;
							status=true;
							break;      
						case 1: 
							plataforma = Plataforma.ON;
							this.error=ErrorType.Ok;
							status=true;
							break;      
						default:
							plataforma = Plataforma.UNKNOW;
							this.error=ErrorType.Invalid;
							status=false;
							break;
					}
				}
				else {  //-- Recibida respuesta invalida
					plataforma = Plataforma.UNKNOW;
					this.error=ErrorType.Invalid;
					status=false;

					//-- Esperar un tiempo y vaciar buffer
					Thread.Sleep(ErrorTimeout);
					this.flush();
				}
				break;
			default : //-- Timeout (u otro error desconocido)
				status=false;
				plataforma = Plataforma.UNKNOW;
				this.error=ErrorType.Timeout;
				break;
		}

		return status;
	}

	/****************************/
	/* PROPIEDADES              */
	/****************************/
	public ErrorType Error 
	{
		get {
			return this.error;
		}
	}

	//***************************************
	//  METODOS PRIVADOS
	//***************************************
	//-- Esperar a recibir una trama de cambio de estado
	private bool Read_cambio(byte[] respuesta)
	{
		//-- Crear la trama
		int n=0;
		int count;
		bool status;

		//-- Esperar a que llegue la respuesta
		//-- Se espera hasta que en el buffer se tengan el numero de bytes
		//-- esperados para la trama. (En el caso de id son 4). Si hay un 
		//-- timeout se aborta
		count=0;
		do {
			//try, catch done because mono-1.2.3 throws an exception when there's a timeout
			//http://bugzilla.gnome.org/show_bug.cgi?id=420520
			bool success = false;
			do {
				try {
					n = sp.Read(respuesta,count,5-count);
					count+=n;
					success = true;
				} catch {}
			} while (!success);
		} while (count<5 && n!=-1);

		//-- Comprobar la respuesta recibida
		switch(count) {
			case 5 : //-- Datos listos
				if (respuesta[0]==(byte)Trama.Evento) {  //-- Trama de evento
					this.error=ErrorType.Ok;
					status=true;
				}
				else {  //-- Recibida trama invalida
					this.error=ErrorType.Invalid;
					status=false;

					//-- Esperar un tiempo y vaciar buffer
					Thread.Sleep(ErrorTimeout);
					this.flush();
				}
				break;
			default : //-- Timeout (u otro error desconocido)
				status=false;
				this.error=ErrorType.Timeout;
				break;
		}

		return status;
	}

	public bool AbortFlush;	

	//-- Vaciar buffer de entrada
	//-- De momento se hace leyendo muchos datos y descartando
	private void flush()
	{
		byte[] buffer = new byte[256];

		//try, catch done because mono-1.2.3 throws an exception when there's a timeout
		//http://bugzilla.gnome.org/show_bug.cgi?id=420520
		bool success = false;
		AbortFlush = false;
		do{
			try{
				sp.Read(buffer,0,256);
				success = true;
				LogB.Debug(" spReaded ");
			} catch {
				LogB.Warning(" catchedTimeOut ");
			}

		} while(! success && ! AbortFlush);
		if(AbortFlush) {
			LogB.Information("Abort flush");
		}
	}

	public void Flush() {
		flush();
	}

	/**********************************/
	/* TIPOS PRIVADOS                 */
	/**********************************/
	//-- Identificacion de las tramas
	private enum Trama
	{
		Evento =  'X',  //-- Trama de evento
		       Estado =  'E',  //-- Trama de solicitud de estado
		       REstado = 'E',  //-- Trama de respuesta de estado
	}

	/*********************************************************************/
	/* CONSTANTES PRIVADAS                                               */
	/*********************************************************************/
	private const int DefaultTimeout = 100;  //-- En ms 
	private const int ErrorTimeout   = 500;  //-- En ms

	//------------------------------
	//   Propiedades privadas
	//------------------------------
	//-- Puerto serie donde esta conectada la Chronopic
	private SerialPort sp;
	//-- Ultimo error que se ha producido
	private ErrorType error = ErrorType.Ok;

}


//methods specific of the Automatic firmware
//for "automatic" firmware 1.1: debounce can change, get version, port scanning
public abstract class ChronopicAuto 
{
	protected SerialPort sp;
	protected int sendNum;
	public bool IsChronopicAuto;
	protected internal abstract string Communicate();
	private string str;
	public string CharToSend = "";
	public bool IsEncoder = false;
	public ChronopicAutoDetect.ChronopicType Found;

	private bool make(SerialPort sp) 
	{
		this.sp = sp;

		if (sp == null) 
			return false;
		
		if (sp != null) 
			if (sp.IsOpen)
				sp.Close(); //close to ensure no bytes are comming

		sp.Open();
			
		if(IsEncoder)
			setEncoderBauds();

		str = "";
		return true;
	}
	private void close(SerialPort sp) {
		sp.Close();
	}	


	//'template method'
	public string Read(SerialPort sp) 
	{
		if ( ! make(sp) )
			return "Error sp == null";
		
		//bool needToFlush = false;
		try {
			str = Communicate();
		} catch {
			//this.error=ErrorType.Timeout;
			LogB.Warning("Error or Timeout. This is not Chronopic-Automatic-Firmware");
			str = "Error / not Multitest firmware";
			
			//needToFlush = true;
		}
		
		/*	
		if(needToFlush)
			flush();
			*/

		close(sp);
		
		return str;
	}
	
	//'template method'
	public string Write(SerialPort sp, int num) 
	{
		if ( ! make(sp) )
			return "Error sp == null";
		
		sendNum = num;
		
		//bool needToFlush = false;
		try {
			str = Communicate();
		} catch {
			//this.error=ErrorType.Timeout;
			LogB.Warning("Error or Timeout. This is not Chronopic-Automatic-Firmware");
			str = "Error / not Multitest firmware";

			//needToFlush = true;
		}
	
		/*	
		if(needToFlush)
			flush();
			*/
		
		close(sp);
		
		return str;
	}

	private void setEncoderBauds()
	{
		sp.BaudRate = 115200; //encoder, 20MHz
		LogB.Information("sp.BaudRate = 115200 bauds");
	}

	/*
	protected void flush() 
	{
		LogB.Information("Flushing");
		
		//-- Esperar un tiempo y vaciar buffer
		Thread.Sleep(500); //ErrorTimeout);
		
		byte[] buffer = new byte[256];
		sp.Read(buffer,0,256); //flush
		
		bool success = false;
		try {
			do{
				sp.Read(buffer,0,256);
				success = true;
				LogB.Debug(" spReaded ");
			} while(! success);
		} catch {
			LogB.Information("Cannot flush");
			return;
		}

		LogB.Information("Flushed");
	}
	*/
}

public class ChronopicAutoCheck : ChronopicAuto
{
	protected internal override string Communicate() 
	{
		Found = ChronopicAutoDetect.ChronopicType.UNDETECTED;
		sp.Write("J");
		IsChronopicAuto = ( (char) sp.ReadByte() == 'J');
		if (IsChronopicAuto) 
		{
			sp.Write("V");
			int major = (char) sp.ReadByte() - '0'; 
			sp.ReadByte(); 		//.
			int minor = (char) sp.ReadByte() - '0'; 

			Found = ChronopicAutoDetect.ChronopicType.NORMAL;
			return "Yes! v" + major.ToString() + "." + minor.ToString();
		}

		return "Please update it\nwith Chronopic-firmwarecord";
	}
}
//only for encoder
public class ChronopicAutoCheckEncoder : ChronopicAuto
{
	protected internal override string Communicate() 
	{
		LogB.Information("Communicate start ...");
		
		Found = ChronopicAutoDetect.ChronopicType.UNDETECTED;
	
		char myByte;
		for(int i = 0; i < 100; i ++) //try 100 times (usually works on Linux 3-5 try, Mac 8-10, Windows don't work < 20... trying bigger numbers)
		{
			LogB.Debug("writting ...");
	
			sp.Write("J");
			
			LogB.Debug("reading ...");

			myByte = (char) sp.ReadByte();
			
			LogB.Debug("readed");
			if(myByte != null && myByte.ToString() != "")
				LogB.Information(myByte.ToString());
			
			if(myByte == 'J') {
				LogB.Information("Encoder found!");

				Found = ChronopicAutoDetect.ChronopicType.ENCODER;
				return "1";
			}
		}
		
		return "0";
	}
}


public class ChronopicAutoCheckDebounce : ChronopicAuto
{
	protected internal override string Communicate() 
	{
		sp.Write("a");
		int debounce = ( sp.ReadByte() - '0' ) * 10;
		return debounce.ToString() + " ms";
	}
}

public class ChronopicAutoChangeDebounce : ChronopicAuto
{
	protected internal override string Communicate() 
	{
		int debounce = sendNum / 10;		//50 -> 5
		
		//byte[] bytesToSend = new byte[2] { 0x62, 0x05 }; //b, 05 //this works
		byte[] bytesToSend = new byte[2] { 0x62, BitConverter.GetBytes(debounce)[0] }; //b, 05
		sp.Write(bytesToSend,0,2);
		
		return "Changed to " + sendNum.ToString() + " ms";
	}
}

public class ChronopicStartReactionTime : ChronopicAuto
{
	protected internal override string Communicate() 
	{
		try {
			sp.Write(CharToSend);
			LogB.Information("sending",CharToSend);
		} catch {
			return "ERROR";
		}
		return "SUCCESS";
	}
}

//like above method but sending the waiting time between each light
public class ChronopicStartReactionTimeAnimation : ChronopicAuto
{
	protected internal override string Communicate() 
	{
		try {
			//values go from 0 to 7
			byte[] bytesToSend = new byte[2] { 0x6c, BitConverter.GetBytes(sendNum)[0] }; //l, 05
			sp.Write(bytesToSend,0,2);
		
		} catch {
			return "ERROR";
		}
		return "SUCCESS";
	}
}

public static class ChronopicPorts
{
	public static string [] GetPorts() {
		if(UtilAll.GetOSEnum() == UtilAll.OperatingSystems.LINUX)
			return Directory.GetFiles("/dev/", "ttyUSB*");
		else if(UtilAll.GetOSEnum() == UtilAll.OperatingSystems.MACOSX)
			return Directory.GetFiles("/dev/", "tty.usbserial*");
		else // WINDOWS
			return SerialPort.GetPortNames();
	}
}

public class ChronopicAutoDetect
{
	public enum ChronopicType { UNDETECTED, NORMAL, ENCODER }
	private ChronopicType searched;

	public string Detected; // portname if detected, if not will be ""

	public ChronopicAutoDetect(ChronopicType type)
	{
		/*
		 * Try to detect a normal 4MHz Chronopic on a 20MHz encoder fails
		 * but encoder can be used normally
		 * In the other hand, try to detect an encoder on a 4MHz Chronopic fails
		 * but encoder cannot be used until 'reset' or disconnect cable (and can be problems with Chronojump GUI)
		 *
		 * So the solution is:
		 * if we are searching encoder, on every port first check if 4MHz connection can be stablished, if it's Found, then normal Chronopic is found
		 * if is not Fount, then search for the encoder.
		 *
		 * The only problem is in normal Chronopics with old firmware (without the 'J' read/write
		 * they will not work after trying to be recognised as an encoder, until reset or disconnect cable
		 *
		 */
		this.searched = type;

		//no matter if we are searching for 4MHz or 20MHz (encoder)
		//first see if 4MHz is connected
		ChronopicAuto caNormal = new ChronopicAutoCheck();
		caNormal.IsEncoder = false;    //for the bauds.

		autoDetect(caNormal);
	}

	private void autoDetect(ChronopicAuto caNormal)
	{
		LogB.Information("starting port detection");

		string [] usbSerial = ChronopicPorts.GetPorts();

		foreach(string port in usbSerial) 
		{
			SerialPort sp = new SerialPort(port);
			
			LogB.Information("searching normal Chronopic at port: ", port);
			string readed = caNormal.Read(sp);

			if(caNormal.Found == ChronopicType.NORMAL) //We found a normal Chronopic
			{
				if(searched == ChronopicType.NORMAL) //normal Chronopic is what we are searching
				{
					Detected = port;
					return;
				} else {
					/*
					 * else: 
					 * means that we are searching for an encoder chronopic and found a normal
					 * so don't try to search for an encoder on that port, because 115200 bauds will saturate it
					 */
					LogB.Information("our goal is to search encoder but found normal Chronopic at port: ", port);
				}
			} else if(searched == ChronopicType.ENCODER) 
			{
				/*
				 * we are searching an encoder
				 * if we arrived here, we know is not a normal chronopic
				 * then wecan search safely for an encoder here
				 */
				ChronopicAuto caEncoder = new ChronopicAutoCheckEncoder();
				caEncoder.IsEncoder = true;    //for the bauds.
			
				LogB.Information("searching encoder Chronopic at port: ", port);
				readed = caEncoder.Read(sp);
				if(caEncoder.Found == ChronopicType.ENCODER) 
				{
					Detected = port;
					return;
				}
			}
		}
		Detected = "";
	}
}

public class ChronopicInit 
{
	public bool CancelledByUser;

	public ChronopicInit () 
	{
	}

	//chronopic init should not touch  gtk, for the threads
	public bool Do (int currentCp, out Chronopic myCp, out SerialPort mySp, Chronopic.Plataforma myPS, string myPort, out string returnString, out bool success) 
	{
		LogB.Information("starting connection with chronopic");

		CancelledByUser = false;
		success = true;
		
		LogB.Information("chronopicInit-1");		
		LogB.Information(string.Format("chronopic port: {0}", myPort));
		mySp = new SerialPort(myPort);
		try {
			mySp.Open();
			LogB.Information("chronopicInit-2");		
			//-- Create chronopic object, for accessing chronopic
			myCp = new Chronopic(mySp);
			
			LogB.Information("chronopicInit-2.1");		
			myCp.Flush();
			
			//if myCp has been cancelled
			if(myCp.AbortFlush) {
				LogB.Information("chronopicInit-2.2 cancelled");
				success = false;
				myCp = new Chronopic(); //fake constructor
			} else {
				LogB.Information("chronopicInit-3");		
				//on windows, this check make a crash 
				//i think the problem is: as we don't really know the Timeout on Windows (.NET) and this variable is not defined on chronopic.cs
				//the Read_platform comes too much soon (when cp is not totally created), and this makes crash

				//-- Obtener el estado inicial de la plataforma

				bool ok=false;
				LogB.Information("chronopicInit-4");		
				do {
					LogB.Information("chronopicInit-5");		
					ok=myCp.Read_platform(out myPS);
					LogB.Information("chronopicInit-6");		
				} while(! ok && ! CancelledByUser);
				LogB.Information("chronopicInit-7");		
				if (!ok) {
					//-- Si hay error terminar
					LogB.Error(string.Format("Error: {0}", myCp.Error));
					success = false;
				}
			}
		} catch {
			LogB.Error("chronopicInit-2.a catched");
			success = false;
			myCp = new Chronopic(); //fake constructor
		}
		
		bool connected = false;
		returnString = "";
		if(success) {
			if(currentCp == 1)
				connected = true;
			returnString = string.Format(Catalog.GetString("<b>Connected</b> to Chronopic on port: {0}"), myPort);
		}
		else {
			returnString = Catalog.GetString("Problems communicating to chronopic.");
			if(currentCp == 1) {
				returnString += " " + Catalog.GetString("Changed platform to 'Simulated'");
				returnString += Catalog.GetString("\n\nWe recommend to remove and connect USB cable.");
			}

			//this will raise on_radiobutton_simulated_ativate and 
			//will put cpRunning to false, and simulated to true and cp.Close()
			if(currentCp == 1) {
				connected = false;
			}
		}

		return connected;
	}
	
}
