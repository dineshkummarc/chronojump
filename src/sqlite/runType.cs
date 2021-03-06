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
 * Copyright (C) 2004-2012   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using System.Data;
using System.IO;
using System.Collections; //ArrayList
using Mono.Data.Sqlite;


class SqliteRunType : Sqlite
{
	public SqliteRunType() {
	}
	
	~SqliteRunType() {}

	/*
	 * create and initialize tables
	 */
	
	//creates table containing the types of simple Runs
	//following INT values are booleans
	//protected internal static void createTableRunType()
	//protected internal static void createTable(string tableName)
	protected override void createTable(string tableName)
	{
		dbcmd.CommandText = 
			//"CREATE TABLE " + Constants.RunTypeTable + " ( " +
			"CREATE TABLE " + tableName + " ( " +
			"uniqueID INTEGER PRIMARY KEY, " +
			"name TEXT, " +
			"distance FLOAT, " + //>0 variable distance, ==0 fixed distance
			"description TEXT )";		
		dbcmd.ExecuteNonQuery();
	}
	
	//if this changes, runType.cs constructor should change 
	//protected internal static void initializeTableRunType()
	protected internal static void initializeTable()
	{
		string [] iniRunTypes = {
			//name:distance:description
			"Custom:0:variable distance running", 
			"20m:20:run 20 meters",
			"100m:100:run 100 meters",
			"200m:200:run 200 meters",
			"400m:400:run 400 meters",
			"1000m:1000:run 1000 meters",
			"2000m:2000:run 2000 meters",
			"Margaria:0:Margaria-Kalamen test",
			"Gesell-DBT:2.5:Gesell Dynamic Balance Test",

			//also simple agility tests
			"Agility-20Yard:18.28:20Yard Agility test",
			"Agility-505:10:505 Agility test",
			"Agility-Illinois:60:Illinois Agility test",
			"Agility-Shuttle-Run:40:Shuttle Run Agility test",
			"Agility-ZigZag:17.6:ZigZag Agility test"
		};
		conversionSubRateTotal = iniRunTypes.Length;
		conversionSubRate = 0;
		foreach(string myString in iniRunTypes) {
			//RunTypeInsert(myString, true);
			conversionSubRate ++;
			string [] s = myString.Split(new char[] {':'});
			RunType type = new RunType();
			type.Name = s[0];
			type.Distance = Convert.ToDouble(Util.ChangeDecimalSeparator(s[1]));
			type.Description = s[2];
			Insert(type, Constants.RunTypeTable, true);
		}
	
		AddGraphLinksRunSimple();	
		AddGraphLinksRunSimpleAgility();	
	}
	
	/*
	 * RunType class methods
	 */

	//public static void RunTypeInsert(string myRun, bool dbconOpened)
	public static int Insert(RunType t, string tableName, bool dbconOpened)
	{
		//string [] myStr = myRun.Split(new char[] {':'});
		if(! dbconOpened) {
			dbcon.Open();
		}
		dbcmd.CommandText = "INSERT INTO " + tableName + 
				" (uniqueID, name, distance, description)" +
				" VALUES (NULL, '" +
				/*
				myStr[0] + "', " + myStr[1] + ", '" +	//name, distance
				myStr[2] + "')" ;	//description
				*/
				t.Name + "', " + Util.ConvertToPoint(t.Distance) + ", '" + t.Description +	"')" ;	
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		//int myLast = dbcon.LastInsertRowId;
		//http://stackoverflow.com/questions/4341178/getting-the-last-insert-id-with-sqlite-net-in-c
		string myString = @"select last_insert_rowid()";
		dbcmd.CommandText = myString;
		int myLast = Convert.ToInt32(dbcmd.ExecuteScalar()); // Need to type-cast since `ExecuteScalar` returns an object.

		if(! dbconOpened) {
			dbcon.Close();
		}
		return myLast;
	}
	
	public static RunType SelectAndReturnRunType(string typeName, bool dbconOpened) 
	{
		if(!dbconOpened)
			dbcon.Open();
		dbcmd.CommandText = "SELECT * " +
			" FROM " + Constants.RunTypeTable +
			" WHERE name  = '" + typeName +
			"' ORDER BY uniqueID";
		
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		RunType myRunType = new RunType();
		
		while(reader.Read()) {
			myRunType.Name = reader[1].ToString();
			myRunType.Distance = Convert.ToDouble( reader[2].ToString() );
			myRunType.Description = reader[3].ToString();
		}
		
		myRunType.IsPredefined = myRunType.FindIfIsPredefined();

		reader.Close();
		if(!dbconOpened)
			dbcon.Close();

		return myRunType;
	}

	public static string[] SelectRunTypes(string allRunsName, bool onlyName) 
	{
		//allRunsName: add and "allRunsName" value
		//onlyName: return only type name
	
		string whereString = "";
		
		dbcon.Open();
		dbcmd.CommandText = "SELECT * " +
			" FROM " + Constants.RunTypeTable +
			whereString +
			" ORDER BY uniqueID";
		
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList myArray = new ArrayList(2);

		int count = new int();
		count = 0;
		while(reader.Read()) {
			if(onlyName) {
				myArray.Add (reader[1].ToString());
			} else {
				myArray.Add (reader[0].ToString() + ":" +	//uniqueID
						reader[1].ToString() + ":" +	//name
						reader[2].ToString() + ":" + 	//distance
						reader[3].ToString() 		//description
					    );
			}
			count ++;
		}

		reader.Close();
		dbcon.Close();

		int numRows;
		if(allRunsName != "") {
			numRows = count +1;
		} else {
			numRows = count;
		}
		string [] myTypes = new string[numRows];
		count =0;
		if(allRunsName != "") {
			myTypes [count++] = allRunsName;
			//Log.WriteLine("{0} - {1}", myTypes[count-1], count-1);
		}
		foreach (string line in myArray) {
			myTypes [count++] = line;
			//Log.WriteLine("{0} - {1}", myTypes[count-1], count-1);
		}

		return myTypes;
	}

	public static double Distance (string typeName) 
	{
		dbcon.Open();
		dbcmd.CommandText = "SELECT distance " +
			" FROM " + Constants.RunTypeTable +
			" WHERE name == '" + typeName + "'";
		
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		double distance = 0;
		while(reader.Read()) {
			distance = Convert.ToDouble(reader[0].ToString());
		}
		reader.Close();
		dbcon.Close();
		return distance;
	}
	
	public static void AddGraphLinksRunSimple() {
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "20m", "run_simple.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "100m", "run_simple.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "200m", "run_simple.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "400m", "run_simple.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "1000m", "run_simple.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "2000m", "run_simple.png", true);
	}

	public static void AddGraphLinksRunSimpleAgility() {
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "Agility-20Yard", "agility_20yard.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "Agility-505", "agility_505.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "Agility-Illinois", "agility_illinois.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "Agility-Shuttle-Run", "agility_shuttle.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "Agility-ZigZag", "agility_zigzag.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "Margaria", "margaria.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunTable, "Gesell-DBT", "gesell_dbt.png", true);
	}


	public static void Delete(string name)
	{
		dbcon.Open();
		dbcmd.CommandText = "Delete FROM " + Constants.RunTypeTable +
			" WHERE name == '" + name + "'";
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();
		dbcon.Close();
	}


}	

class SqliteRunIntervalType : SqliteRunType
{
	public SqliteRunIntervalType() {
	}
	
	~SqliteRunIntervalType() {}
	
	//creates table containing the types of Interval Runs 
	//following INT values are booleans
	//protected internal static void createTableRunIntervalType()
	//protected internal static void createTable(string tableName)
	protected override void createTable(string tableName)
	{
		dbcmd.CommandText = 
			//"CREATE TABLE " + Constants.RunIntervalTypeTable + " ( " +
			"CREATE TABLE " + tableName + " ( " +
			"uniqueID INTEGER PRIMARY KEY, " +
			"name TEXT, " +
			"distance FLOAT, " + //>0 variable distance, ==0 fixed distance
					//this distance will be the same in all tracks.
					//-1 each track can have a different distance (started at 0.8.1.5, see distancesString)
			"tracksLimited INT, " +  //1 limited by tracks (intervals); 0 limited by time
			"fixedValue INT, " +   //0: no fixed value; 3: 3 tracks or seconds 
			"unlimited INT, " +		
			"description TEXT, " +	
			"distancesString TEXT )"; 	//new at 0.8.1.5:
		       					//when distance is 0 or >0, distancesString it's ""
							//when distance is -1, distancesString is distance of each track, 
							//	eg: "7-5-9" for a runInterval with three tracks of 7, 5 and 9 meters each
							//	this is nice for agility tests
		dbcmd.ExecuteNonQuery();
	}
	
	//if this changes, runType.cs constructor should change 
	//protected internal static void initializeTableRunIntervalType()
	protected internal static new void initializeTable()
	{
		string [] iniRunTypes = {
			//name:distance:tracksLimited:fixedValue:unlimited:description
			"byLaps:0:1:0:0:Run n laps x distance:",
			"byTime:0:0:0:0:Make max laps in n seconds:",
			"unlimited:0:0:0:1:Continue running in n distance:",	//suppose limited by time
			"20m10times:20:1:10:0:Run 10 times a 20m distance:",	//only in more runs
			"7m30seconds:7:0:30:0:Make max laps in 30 seconds:",	//only in more runs
			"20m endurance:20:0:0:1:Continue running in 20m distance:",	//only in more runs
			"MTGUG:-1:1:3:0:Modified time Getup and Go test:1-7-19",
			"RSA 8-4-R2-5:-1:1:4:0:RSA testing:8-4-R3-5"
		};
		foreach(string myString in iniRunTypes) {
			//RunIntervalTypeInsert(myString, true);
			string [] s = myString.Split(new char[] {':'});
			RunType type = new RunType();
			type.Name = s[0];
			type.Distance = Convert.ToDouble(Util.ChangeDecimalSeparator(s[1]));
			type.TracksLimited = Util.IntToBool(Convert.ToInt32(s[2]));
			type.FixedValue = Convert.ToInt32(s[3]);
			type.Unlimited = Util.IntToBool(Convert.ToInt32(s[4]));
			type.Description = s[5];
			type.DistancesString = s[6];
			Insert(type, Constants.RunIntervalTypeTable, true);
		}
		
		AddGraphLinksRunInterval();	
	}

	//public static void RunIntervalTypeInsert(string myRun, bool dbconOpened)
	public static new int Insert(RunType t, string tableName, bool dbconOpened)
	{
		//done here for not having twho dbconsOpened
		//double distance = t.Distance;

		//string [] myStr = myRun.Split(new char[] {':'});
		if(! dbconOpened) {
			dbcon.Open();
		}
		dbcmd.CommandText = "INSERT INTO " + tableName + 
				" (uniqueID, name, distance, tracksLimited, fixedValue, unlimited, description, distancesString)" +
				" VALUES (NULL, '" +
				/*
				myStr[0] + "', " + myStr[1] + ", " +	//name, distance
				myStr[2] + ", " + myStr[3] + ", " +	//tracksLimited, fixedValue
				myStr[4] + ", '" + myStr[5] + ", " +	//unlimited, description
				myStr[6] + "')" ;			//distancesString
				*/
				//t.Name + 	"', " + distance + ", " + t.TracksLimited + 	", " + t.FixedValue + ", " +
				t.Name + 	"', " + t.Distance + ", " + Util.BoolToInt(t.TracksLimited) + 	", " + t.FixedValue + ", " +
				Util.BoolToInt(t.Unlimited) + 	", '" + t.Description +	"', '" + t.DistancesString + 	"')" ;	
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();
		
		//int myLast = dbcon.LastInsertRowId;
		//http://stackoverflow.com/questions/4341178/getting-the-last-insert-id-with-sqlite-net-in-c
		string myString = @"select last_insert_rowid()";
		dbcmd.CommandText = myString;
		int myLast = Convert.ToInt32(dbcmd.ExecuteScalar()); // Need to type-cast since `ExecuteScalar` returns an object.

		if(! dbconOpened) {
			dbcon.Close();
		}
		return myLast;
	}

	public static string[] SelectRunIntervalTypes(string allRunsName, bool onlyName) 
	{
		dbcon.Open();
		dbcmd.CommandText = "SELECT * " +
			" FROM " + Constants.RunIntervalTypeTable +
			" ORDER BY uniqueID";
		
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList myArray = new ArrayList(2);

		int count = new int();
		count = 0;
		while(reader.Read()) {
			if(onlyName) {
				myArray.Add (reader[1].ToString());
			} else {
				myArray.Add (reader[0].ToString() + ":" +	//uniqueID
						reader[1].ToString() + ":" +	//name
						reader[2].ToString() + ":" + 	//distance
						reader[3].ToString() + ":" + 	//tracksLimited
						reader[4].ToString() + ":" + 	//fixedValue
						reader[5].ToString() + ":" + 	//unlimited
						reader[6].ToString() + ":" +	//description
						Util.ChangeDecimalSeparator(reader[7].ToString())	//distancesString
					    );
			}
			count ++;
		}

		reader.Close();
		dbcon.Close();

		int numRows;
		if(allRunsName != "") {
			numRows = count +1;
		} else {
			numRows = count;
		}
		string [] myTypes = new string[numRows];
		count =0;
		if(allRunsName != "") {
			myTypes [count++] = allRunsName;
		}
		foreach (string line in myArray) {
			myTypes [count++] = line;
		}

		return myTypes;
	}

	public static RunType SelectAndReturnRunIntervalType(string typeName, bool dbconOpened) 
	{
		if(!dbconOpened)
			dbcon.Open();
		dbcmd.CommandText = "SELECT * " +
			" FROM " + Constants.RunIntervalTypeTable +
			" WHERE name  = '" + typeName +
			"' ORDER BY uniqueID";
		
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		RunType myRunType = new RunType();
		
		while(reader.Read()) {
			myRunType.Name = reader[1].ToString();
			myRunType.Distance = Convert.ToDouble( reader[2].ToString() );
			myRunType.HasIntervals = true;
			myRunType.TracksLimited = Util.IntToBool(Convert.ToInt32(reader[3].ToString()));
			myRunType.FixedValue = Convert.ToInt32( reader[4].ToString() );
			myRunType.Unlimited = Util.IntToBool(Convert.ToInt32(reader[5].ToString()));
			myRunType.Description = reader[6].ToString();
			myRunType.DistancesString = Util.ChangeDecimalSeparator(reader[7].ToString());
		}

		myRunType.IsPredefined = myRunType.FindIfIsPredefined();

		reader.Close();
		if(!dbconOpened)
			dbcon.Close();

		return myRunType;
	}

	public static void AddGraphLinksRunInterval() {
		SqliteEvent.GraphLinkInsert (Constants.RunIntervalTable, "byLaps", "run_interval.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunIntervalTable, "byTime", "run_interval.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunIntervalTable, "unlimited", "run_interval.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunIntervalTable, "20m10times", "run_interval.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunIntervalTable, "7m30seconds", "run_interval.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunIntervalTable, "20m endurance", "run_interval.png", true);
		SqliteEvent.GraphLinkInsert (Constants.RunIntervalTable, "MTGUG", "mtgug.png", true);
	}
	
	public static void Delete(string name)
	{
		dbcon.Open();
		dbcmd.CommandText = "Delete FROM " + Constants.RunIntervalTypeTable +
			" WHERE name == '" + name + "'";
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();
		dbcon.Close();
	}


}
