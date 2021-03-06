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
 * Copyright (C) 2004-2010   Xavier de Blas <xaviblas@gmail.com> 
 */

using System;
using System.Data;
using System.IO;
using System.Collections; //ArrayList
using Mono.Data.Sqlite;


class SqlitePerson : Sqlite
{
	public SqlitePerson() {
	}
	
	~SqlitePerson() {}

	//can be "Constants.PersonTable" or "Constants.ConvertTempTable"
	//temp is used to modify table between different database versions if needed
	//protected new internal static void createTable(string tableName)
	protected override void createTable(string tableName)
	 {
		dbcmd.CommandText = 
			"CREATE TABLE " + tableName + " ( " +
			"uniqueID INTEGER PRIMARY KEY, " +
			"name TEXT, " +
			"sex TEXT, " +
			"dateborn TEXT, " + //YYYY-MM-DD since db 0.72
			"race INT, " + 
			"countryID INT, " + 
			"description TEXT, " +	
			"future1 TEXT, " +	
			"future2 TEXT, " +	
			"serverUniqueID INT ) ";
		dbcmd.ExecuteNonQuery();
	 }

	public static int Insert(bool dbconOpened, string uniqueID, string name, string sex, DateTime dateBorn, 
			int race, int countryID, string description, int serverUniqueID)
	{
		if(! dbconOpened)
			dbcon.Open();

		if(uniqueID == "-1")
			uniqueID = "NULL";

		string myString = "INSERT INTO " + Constants.PersonTable + 
			" (uniqueID, name, sex, dateBorn, race, countryID, description, future1, future2, serverUniqueID) VALUES (" + uniqueID + ", '" +
			name + "', '" + sex + "', '" + UtilDate.ToSql(dateBorn) + "', " + 
			race + ", " + countryID + ", '" + description + "', '', '', " + serverUniqueID + ")";
		
		dbcmd.CommandText = myString;
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		//int myLast = -10000; //dbcon.LastInsertRowId;
		//http://stackoverflow.com/questions/4341178/getting-the-last-insert-id-with-sqlite-net-in-c
		myString = @"select last_insert_rowid()";
		dbcmd.CommandText = myString;
		int myLast = Convert.ToInt32(dbcmd.ExecuteScalar()); // Need to type-cast since `ExecuteScalar` returns an object.

		if(! dbconOpened)
			dbcon.Close();

		return myLast;
	}

	//This is like SqlitePersonSession.Selectbut this returns a Person
	public static Person Select(int uniqueID)
	{
		dbcon.Open();

		dbcmd.CommandText = "SELECT * FROM " + Constants.PersonTable + " WHERE uniqueID == " + uniqueID;
		
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();
		
		Person p = new Person(-1);
		if(reader.Read()) {
			p = new Person(
					Convert.ToInt32(reader[0].ToString()), //uniqueID
					reader[1].ToString(), 			//name
					reader[2].ToString(), 			//sex
					UtilDate.FromSql(reader[3].ToString()),//dateBorn
					Convert.ToInt32(reader[4].ToString()), //race
					Convert.ToInt32(reader[5].ToString()), //countryID
					reader[6].ToString(), 			//description
					Convert.ToInt32(reader[9].ToString()) //serverUniqueID
					);
		}
		reader.Close();
		dbcon.Close();
		return p;
	}
		
	//public static string SelectJumperName(int uniqueID)
	//select strings
	public static string SelectAttribute(int uniqueID, string attribute)
	{
		dbcon.Open();

		dbcmd.CommandText = "SELECT " + attribute + " FROM " + Constants.PersonTable + " WHERE uniqueID == " + uniqueID;
		
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();

		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();
		
		string myReturn = "";
		if(reader.Read()) {
			myReturn = reader[0].ToString();
		}
		reader.Close();
		dbcon.Close();
		return myReturn;
	}
		
	//currently only used on server
	public static ArrayList SelectAllPersons() 
	{
		dbcon.Open();
		dbcmd.CommandText = "SELECT uniqueID, name FROM " + Constants.PersonTable; 
		
		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList myArray = new ArrayList(1);

		while(reader.Read()) 
			myArray.Add ("(" + reader[0].ToString() + ") " + reader[1].ToString());

		reader.Close();
		dbcon.Close();

		return myArray;
	}
		
	public static ArrayList SelectAllPersonsRecuperable(string sortedBy, int except, int inSession, string searchFilterName) 
	{
		//sortedBy = name or uniqueID (= creation date)
	

		//1st select all the person.uniqueID of people who are in CurrentSession (or none if except == -1)
		//2n select all names in database (or in one session if inSession != -1)
		//3d filter all names (save all found in 2 that is not in 1)
		//
		//probably this can be made in only one time... future
		//
		//1
		
		string tp = Constants.PersonTable;
		string tps = Constants.PersonSessionTable;

		dbcon.Open();
		dbcmd.CommandText = "SELECT " + tp + ".uniqueID " +
			" FROM " + tp + "," + tps +
			" WHERE " + tps + ".sessionID == " + except + 
			" AND " + tp + ".uniqueID == " + tps + ".personID "; 
		
		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();

		ArrayList arrayExcept = new ArrayList(2);

		while(reader.Read()) 
			arrayExcept.Add (reader[0].ToString());

		reader.Close();
		dbcon.Close();
		
		//2
		//sort no case sensitive when we sort by name
		if(sortedBy == "name") { 
			sortedBy = "lower(" + tp + ".name)" ; 
		} else { 
			sortedBy = tp + ".uniqueID" ; 
		}
		
		dbcon.Open();
		if(inSession == -1) {
			string nameLike = "";
			if(searchFilterName != "")
				nameLike = " WHERE LOWER(" + tp + ".name) LIKE LOWER ('%" + searchFilterName + "%') ";

			dbcmd.CommandText = 
				"SELECT * FROM " + tp + 
				nameLike + 
				" ORDER BY " + sortedBy;

		} else {
			dbcmd.CommandText = 
				"SELECT " + tp + ".* FROM " + tp + ", " + tps + 
				" WHERE " + tps + ".sessionID == " + inSession + 
				" AND " + tp + ".uniqueID == " + tps + ".personID " + 
				" ORDER BY " + sortedBy;
		}
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		SqliteDataReader reader2;
		reader2 = dbcmd.ExecuteReader();

		ArrayList arrayReturn = new ArrayList(2);

		bool found;

		//3
		while(reader2.Read()) {
			found = false;
			foreach (string line in arrayExcept) {
				if(line == reader2[0].ToString()) {
					found = true;
					goto finishForeach;
				}
			}
			
finishForeach:
			
			if (!found) {
				Person p = new Person(
						Convert.ToInt32(reader2[0].ToString()), //uniqueID
						reader2[1].ToString(), 			//name
						reader2[2].ToString(), 			//sex
						UtilDate.FromSql(reader2[3].ToString()),//dateBorn
						Convert.ToInt32(reader2[4].ToString()), //race
						Convert.ToInt32(reader2[5].ToString()), //countryID
						reader2[6].ToString(), 			//description
						Convert.ToInt32(reader2[9].ToString()) //serverUniqueID
						);
				arrayReturn.Add(p);
			}
		}

		reader2.Close();
		dbcon.Close();

		return arrayReturn;
	}

	public static ArrayList SelectAllPersonEvents(int personID) 
	{
		SqliteDataReader reader;
		ArrayList arraySessions = new ArrayList(2);
		ArrayList arrayJumps = new ArrayList(2);
		ArrayList arrayJumpsRj = new ArrayList(2);
		ArrayList arrayRuns = new ArrayList(2);
		ArrayList arrayRunsInterval = new ArrayList(2);
		ArrayList arrayRTs = new ArrayList(2);
		ArrayList arrayPulses = new ArrayList(2);
		ArrayList arrayMCs = new ArrayList(2);
		
		string tps = Constants.PersonSessionTable;
	
		dbcon.Open();
		
		//session where this person is loaded
		dbcmd.CommandText = "SELECT sessionID, session.Name, session.Place, session.Date " + 
			" FROM " + tps + ", session " + 
			" WHERE personID = " + personID + " AND session.uniqueID == " + tps + ".sessionID " +
			" ORDER BY sessionID";
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		reader = dbcmd.ExecuteReader();
		while(reader.Read()) {
			arraySessions.Add ( reader[0].ToString() + ":" + reader[1].ToString() + ":" +
					reader[2].ToString() + ":" + 
					UtilDate.FromSql(reader[3].ToString()).ToShortDateString()
					);
		}
		reader.Close();

		
		//jumps
		dbcmd.CommandText = "SELECT sessionID, count(*) FROM jump WHERE personID = " + personID +
			" GROUP BY sessionID ORDER BY sessionID";
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		reader = dbcmd.ExecuteReader();
		while(reader.Read()) {
			arrayJumps.Add ( reader[0].ToString() + ":" + reader[1].ToString() );
		}
		reader.Close();
		
		//jumpsRj
		dbcmd.CommandText = "SELECT sessionID, count(*) FROM jumpRj WHERE personID = " + personID +
			" GROUP BY sessionID ORDER BY sessionID";
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		reader = dbcmd.ExecuteReader();
		while(reader.Read()) {
			arrayJumpsRj.Add ( reader[0].ToString() + ":" + reader[1].ToString() );
		}
		reader.Close();
		
		//runs
		dbcmd.CommandText = "SELECT sessionID, count(*) FROM run WHERE personID = " + personID +
			" GROUP BY sessionID ORDER BY sessionID";
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		reader = dbcmd.ExecuteReader();
		while(reader.Read()) {
			arrayRuns.Add ( reader[0].ToString() + ":" + reader[1].ToString() );
		}
		reader.Close();
		
		//runsInterval
		dbcmd.CommandText = "SELECT sessionID, count(*) FROM runInterval WHERE personID = " + personID +
			" GROUP BY sessionID ORDER BY sessionID";
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		reader = dbcmd.ExecuteReader();
		while(reader.Read()) {
			arrayRunsInterval.Add ( reader[0].ToString() + ":" + reader[1].ToString() );
		}
		reader.Close();
		
		//reaction time
		dbcmd.CommandText = "SELECT sessionID, count(*) FROM reactiontime WHERE personID = " + personID +
			" GROUP BY sessionID ORDER BY sessionID";
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		reader = dbcmd.ExecuteReader();
		while(reader.Read()) {
			arrayRTs.Add ( reader[0].ToString() + ":" + reader[1].ToString() );
		}
		reader.Close();
	
		//pulses
		dbcmd.CommandText = "SELECT sessionID, count(*) FROM pulse WHERE personID = " + personID +
			" GROUP BY sessionID ORDER BY sessionID";
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		reader = dbcmd.ExecuteReader();
		while(reader.Read()) {
			arrayPulses.Add ( reader[0].ToString() + ":" + reader[1].ToString() );
		}
		reader.Close();
	
		//pulses
		dbcmd.CommandText = "SELECT sessionID, count(*) FROM multiChronopic WHERE personID = " + personID +
			" GROUP BY sessionID ORDER BY sessionID";
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		reader = dbcmd.ExecuteReader();
		while(reader.Read()) {
			arrayMCs.Add ( reader[0].ToString() + ":" + reader[1].ToString() );
		}
		reader.Close();
	
	
		dbcon.Close();
		
	
		ArrayList arrayAll = new ArrayList(2);
		string tempJumps;
		string tempJumpsRj;
		string tempRuns;
		string tempRunsInterval;
		string tempRTs;
		string tempPulses;
		string tempMCs;
		bool found; 	//using found because a person can be loaded in a session 
				//but whithout having done any event yet

		//foreach session where this jumper it's loaded, check which events has
		foreach (string mySession in arraySessions) {
			string [] myStrSession = mySession.Split(new char[] {':'});
			tempJumps = "";
			tempJumpsRj = "";
			tempRuns = "";
			tempRunsInterval = "";
			tempRTs = "";
			tempPulses = "";
			tempMCs = "";
			found = false;
			
			foreach (string myJumps in arrayJumps) {
				string [] myStr = myJumps.Split(new char[] {':'});
				if(myStrSession[0] == myStr[0]) {
					tempJumps = myStr[1];
					found = true;
					break;
				}
			}
		
			foreach (string myJumpsRj in arrayJumpsRj) {
				string [] myStr = myJumpsRj.Split(new char[] {':'});
				if(myStrSession[0] == myStr[0]) {
					tempJumpsRj = myStr[1];
					found = true;
					break;
				}
			}
			
			foreach (string myRuns in arrayRuns) {
				string [] myStr = myRuns.Split(new char[] {':'});
				if(myStrSession[0] == myStr[0]) {
					tempRuns = myStr[1];
					found = true;
					break;
				}
			}
			
			foreach (string myRunsInterval in arrayRunsInterval) {
				string [] myStr = myRunsInterval.Split(new char[] {':'});
				if(myStrSession[0] == myStr[0]) {
					tempRunsInterval = myStr[1];
					found = true;
					break;
				}
			}
			
			foreach (string myRTs in arrayRTs) {
				string [] myStr = myRTs.Split(new char[] {':'});
				if(myStrSession[0] == myStr[0]) {
					tempRTs = myStr[1];
					found = true;
					break;
				}
			}
			
			foreach (string myPulses in arrayPulses) {
				string [] myStr = myPulses.Split(new char[] {':'});
				if(myStrSession[0] == myStr[0]) {
					tempPulses = myStr[1];
					found = true;
					break;
				}
			}
			
			foreach (string myMCs in arrayMCs) {
				string [] myStr = myMCs.Split(new char[] {':'});
				if(myStrSession[0] == myStr[0]) {
					tempMCs = myStr[1];
					found = true;
					break;
				}
			}


			//if has events, write it's data
			if (found) {
				arrayAll.Add (myStrSession[1] + ":" + myStrSession[2] + ":" + 	//session name, place
						myStrSession[3] + ":" + tempJumps + ":" + 	//sessionDate, jumps
						tempJumpsRj + ":" + tempRuns + ":" + 		//jumpsRj, Runs
						tempRunsInterval + ":" + tempRTs + ":" + 	//runsInterval, Reaction times
						tempPulses + ":" + tempMCs);			//pulses, MultiChronopic
			}
		}

		return arrayAll;
	}
	
	public static bool ExistsAndItsNotMe(int uniqueID, string personName)
	{
		dbcon.Open();
		dbcmd.CommandText = "SELECT uniqueID FROM " + Constants.PersonTable +
			" WHERE LOWER(" + Constants.PersonTable + ".name) == LOWER('" + personName + "')" +
			" AND uniqueID != " + uniqueID ;
		Log.WriteLine(dbcmd.CommandText.ToString());
		
		SqliteDataReader reader;
		reader = dbcmd.ExecuteReader();
	
		bool exists = new bool();
		exists = false;
		
		if (reader.Read()) {
			exists = true;
			//Log.WriteLine("valor {0}", reader[0].ToString());
		}
		//Log.WriteLine("exists = {0}", exists.ToString());

		reader.Close();
		dbcon.Close();
		return exists;
	}
	
	
	public static void Update(Person myPerson)
	{
		dbcon.Open();
		dbcmd.CommandText = "UPDATE " + Constants.PersonTable + 
			" SET name = '" + myPerson.Name + 
			"', sex = '" + myPerson.Sex +
			"', dateborn = '" + UtilDate.ToSql(myPerson.DateBorn) +
			"', race = " + myPerson.Race +
			", countryID = " + myPerson.CountryID +
			", description = '" + myPerson.Description +
			"', serverUniqueID = " + myPerson.ServerUniqueID +
			" WHERE uniqueID == " + myPerson.UniqueID;
		Log.WriteLine(dbcmd.CommandText.ToString());
		dbcmd.ExecuteNonQuery();
		dbcon.Close();
	}

}
