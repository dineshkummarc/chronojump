/*
 * This file is part of ChronoJump
 *
 * ChronoJump is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or   
 *    (at your option) any later version.
 *    
 * ChronoJump is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 *    GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 * Xavier de Blas: 
 * http://www.xdeblas.com, http://www.deporteyciencia.com (parleblas)
 */

using System;
using System.Data;
using System.Text; //StringBuilder

//this class tries to be a space for methods that are used in different classes
public class Util
{
	public static string TrimDecimals (string time, int prefsDigitsNumber) {
		//the +2 is a workarround for not counting the two first characters: "0."
		//this will not work with the fall
		if(time == "-1") {
			return "-";
		} else {
			return time.Length > prefsDigitsNumber + 2 ? 
				time.Substring( 0, prefsDigitsNumber + 2 ) : 
					time;
		}
	}
	
	public static double GetMax (string values)
	{
		string [] myStringFull = values.Split(new char[] {'='});
		double max = 0;
		foreach (string jump in myStringFull) {
			if ( Convert.ToDouble(jump) > max ) {
				max = Convert.ToDouble(jump);
			}
		}
		return max ; 
	}
	
	public static double GetAverage (string values)
	{
		string [] myStringFull = values.Split(new char[] {'='});
		double myAverage = 0;
		double myCount = 0;
		foreach (string jump in myStringFull) {
			//if there's a -1 value, should not be counted in the averages
			if(Convert.ToDouble(jump) != -1) {
				myAverage = myAverage + Convert.ToDouble(jump);
				myCount ++;
			}
		}
		return myAverage / myCount ; 
	}

	//useful for jumpType and jumpRjType, because the third value is the same
	public static bool HasWeight(string [] jumpTypes, string myType) {
		foreach (string myString in jumpTypes) {
			string [] myStringFull = myString.Split(new char[] {':'});
			if(myStringFull[1] == myType) {
				if(myStringFull[3] == "1") { return true;
				} else { return false;
				}
			}
		}
		Console.WriteLine("Error, myType: {0} not found", myType);
		return false;
	}
	
	//useful for jumpType and jumpRjType, because the second value is the same
	public static bool HasFall(string [] jumpTypes, string myType) {
		foreach (string myString in jumpTypes) {
			string [] myStringFull = myString.Split(new char[] {':'});
			if(myStringFull[1] == myType) {
				if(myStringFull[2] == "0") { return true;
				} else { return false;
				}
			}
		}
		Console.WriteLine("Error, myType: {0} not found", myType);
		return false;
	}

	public static string RemoveTilde(string myString) 
	{
		StringBuilder myStringBuilder = new StringBuilder(myString);
		myStringBuilder.Replace("'", "");
		return myStringBuilder.ToString();
	}
	
	public static string RemoveTildeAndColon(string myString) 
	{
		StringBuilder myStringBuilder = new StringBuilder(myString);
		myStringBuilder.Replace("'", "");
		myStringBuilder.Replace(":", "");
		return myStringBuilder.ToString();
	}
	
	//dot is used for separating sex in stats names (cannot be used for a new jumpType)
	public static string RemoveTildeAndColonAndDot(string myString) 
	{
		StringBuilder myStringBuilder = new StringBuilder(myString);
		myStringBuilder.Replace("'", "");
		myStringBuilder.Replace(":", "");
		myStringBuilder.Replace(".", "");
		return myStringBuilder.ToString();
	}

	public static string ObtainHeightInCentimeters (string time) {
		// s = 4.9 * (tv/2)exp2
		double timeAsDouble = Convert.ToDouble(time);
		double height = 100 * 4.9 * ( timeAsDouble / 2 ) * ( timeAsDouble / 2 ) ;

		return height.ToString();
	}
	
	public static int GetNumberOfJumps(string myString)
	{
		if(myString.Length > 0) {
			string [] jumpsSeparated = myString.Split(new char[] {'='});
			int count = 0;
			foreach (string temp in jumpsSeparated) {
				count++;
			}
			if(count == 0) { count =1; }
			
			return count;
		} else { 
			return 0;
		}
	}
	
	public static double GetTotalTime (string stringTC, string stringTV)
	{
		if(stringTC.Length > 0 && stringTV.Length > 0) {
			string [] tc = stringTC.Split(new char[] {'='});
			string [] tv = stringTV.Split(new char[] {'='});

			double totalTime = 0;

			foreach (string jump in tc) {
				totalTime = totalTime + Convert.ToDouble(jump);
			}
			foreach (string jump in tv) {
				totalTime = totalTime + Convert.ToDouble(jump);
			}

			return totalTime ;
		} else {
			return 0;
		}
	}
	
	public static string FetchID (string text)
	{
		string [] myStringFull = text.Split(new char[] {':'});
		return myStringFull[0];
	}
	
	public static string FetchName (string text)
	{
		//"id: name" (return only name)
		bool found = false;
		int i;
		for (i=0; ! found ; i++) {
			if(text[i] == ' ') {
				found = true;
			}
		}
		return text.Substring(i);
	}

	//in ubuntu (not in debian) found problems between the ',' and the '.'
	//in mono and sqlite, try to solve with this
	//but do it later (when found a global solution)
	//now continue solving errors writing:
	//export LANG=C
	//before executing chronojump
	//it's a pity because we loose translations
	public static string ConvertToPoint (double myDouble)
	{
		StringBuilder myStringBuilder = new StringBuilder(myDouble.ToString());
		myStringBuilder.Replace(",", ".");
		return myStringBuilder.ToString();
	}
}

