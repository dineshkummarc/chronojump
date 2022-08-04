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
 * Copyright (C) 2022 Xavier de Blas
 */

using System;
using System.Net;
using System.IO;
using System.Json;
using System.Text;
using System.Collections;
using System.Collections.Generic; //List<>

public class JsonCompujumpJumps : JsonCompujump
{
	public JsonCompujumpJumps (bool django)
	{
		this.django = django;

		ResultMessage = "";
	}

	public override void GetJumpStationExercises (int stationId)
	{
		JumpSimpleExercises_l = new List<SelectJumpTypes> ();
		JumpRjExercises_l = new List<SelectJumpRjTypes> ();

		// Create a request using a URL that can receive a post.
		if (! createWebRequest(requestType.AUTHENTICATED, "/api/v1/client/getStationExercises"))
			return;

		// Set the Method property of the request to GET.
		request.Method = "GET";

		HttpWebResponse response;
		if(! getHttpWebResponse (request, out response, exercisesFailedStr))
			return;

		string responseFromServer;
		using (var sr = new StreamReader(response.GetResponseStream()))
		{
			responseFromServer = sr.ReadToEnd();
		}

		LogB.Information("GetStationExercises: " + responseFromServer);

		if(responseFromServer == "")
			LogB.Information(" Empty "); //never happens
		else if(responseFromServer == "[]")
			LogB.Information(" Empty2 "); //when rfid is not on server
		else
			stationExercisesDeserialize(responseFromServer, stationId);
	}

	private void stationExercisesDeserialize (string str, int stationId)
	{
		JsonValue jsonStationExercises = JsonValue.Parse (str);

		foreach (JsonValue jsonSE in jsonStationExercises)
		{
			// 2) discard if is not for this station type
			string type = jsonSE ["measurable"];
			if(type != "J") //TODO: check jumpsMultiple
				continue;

			// 3) add exercise to the list
			Int32 newExId = jsonSE ["id"];
			string newExName = jsonSE ["name"];

			/*
			   on the server put percent_additional_body_weight as 0 for sj and 100 for lj
			   then here the 100 will be a hasWeight = true
			   and user maybe can change it
			   */
			bool hasWeight = false;
			if (jsonSE ["measurable_info"]["percent_additional_body_weight"] == 100)
				hasWeight = true;

			/*
			//the description is not being downloaded and the comment is not saved in the server
			string comment = "";
			JsonValue jsonSEStations = JsonValue.Parse (jsonSE["stations"].ToString());
			foreach (JsonValue jsonSEStation in jsonSEStations)
				comment = jsonSEStation ["comment"];
			*/

			//TODO: think on unlimited jumps
			if (
					(jsonSE ["measurable_info"]["jump_limit"] != null &&
					 jsonSE ["measurable_info"]["jump_limit"] > 1) ||
					(jsonSE ["measurable_info"]["time_limit"] != null &&
					 jsonSE ["measurable_info"]["time_limit"] > 1) )
			{
				bool jumpsLimited;
				double fixedValue;
				if (jsonSE ["measurable_info"]["jump_limit"] != null &&
						jsonSE ["measurable_info"]["jump_limit"] > 1)
				{
					jumpsLimited = true;
					fixedValue = jsonSE ["measurable_info"]["jump_limit"];
				} else {
					jumpsLimited = false;
					fixedValue = jsonSE ["measurable_info"]["time_limit"];
				}

				JumpRjExercises_l.Add (new SelectJumpRjTypes (newExId, newExName,
							(bool) jsonSE ["measurable_info"]["start_inside"],
							hasWeight, jumpsLimited, fixedValue, ""));
			}
			else
				JumpSimpleExercises_l.Add (new SelectJumpTypes (newExId, newExName,
							(bool) jsonSE ["measurable_info"]["start_inside"],
							hasWeight, ""));
		}
	}
}