/*
 * This file is part of ChronoJump
 *
 * Chronojump is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or   
 *    (at your option) any later version.
 *    
 * Chronojump is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 *    GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 * Copyright (C) 2008   Sharad Shankar & Onkar Nath Mishra http://www.logicbrick.com/
 * Copyright (C) 2008-2010  Xavier de Blas <xaviblas@gmail.com> 
 *
 * version: 1.6 (Nov, 12, 2008)
 */

/*
 * This is suitable to detect minimum angle on the flexion previous to a CMJ jump
 * check samples working:
 * http://vimeo.com/album/28658
 *
 * more info here:
 * http://mail.gnome.org/archives/chronojump-list/2008-July/msg00005.html
 */

/*
 * CONSTRAINTS:
 * 
 * -Person have to be "looking" to the right of the camera
 * -Camera view area will be (having jumper stand up):
 *   below: the toe preferrably has not to be shown
 *   above: the top part of the image will be the right hand (fully included)
 * -Black trousers should be use. Rest of the clothes should not be black.
 * -White background is highly recommended
 * -Initially user should stand straight for 1-2 frames so that some values can be set by the program. 
 *
 */


/*
 * EXPLANATION:
 *
 * The incoming image is converted into Gray form of image and a fixed low level threshold 
 * is applied to  the grayscale image.
 *
 * Now the call to the function *findLargestContour* finds the bounding rectangle
 * of the largest connected contour in the thresholded image.This function returns
 * the bounding rectangle of the largest contours and draws the largest contour on a temporay image.
 *
 * Now the call to the function *findHipPoint* find the x coordinate of the pixel fulfilling
 * following criteria in the image containing maximum bounding rectangle :
 * 
 * 1.white pixel with minimum x coordinate
 * This gives the coordinate of the Hip of the person.
 *
 * Now the call to the function *findKneePoint* takes as argument the bounding rectangle
 * of the largest  connected contours and the y coordinate of the hippoint calculated in the *findHipPoint*.
 * The function returns the x coordinate of the white pixel having maximum x coordinate below the hip point. 
 * The white pixel with largest x coordinate below the hip point gives the coordinate of the Knee point of the leg.
 *
 * Now the call to the function *findToePoint* searches for the white pixels below the knee point
 * and having minimum x coordinate.This gives the coordinate of the toe point.
 *
 * Minimum Angle Calculation: 
 * The x coordinate of the hip point is increased by 10 and the x coordinate of the knee point
 * and toe point is decreased by 10 to get the exact hip,knee and toe points.
 * The angle between hip to knee line and knee to toe line is calculated for all the input frames
 * and the minimum value of these angles give the minimum angle.
 */

/*
 * COMPILATION:
 *
 * Ubuntu 9.10, now using RInside:
 *
 * g++ -I/usr/include/opencv -I/usr/share/R/include -I/usr/local/lib/R/site-library/Rcpp/lib -I/usr/local/lib/R/site-library/RInside/lib  -g -O2 -Wall  -s  kneeAngle.cpp -o kneeAngle -L/usr/lib/R/lib -lR -lblas -llapack -L/usr/local/lib/R/site-library/Rcpp/lib -lRcpp -Wl,-rpath,/usr/local/lib/R/site-library/Rcpp/lib -L/usr/local/lib/R/site-library/RInside/lib -lRInside -Wl,-rpath,/usr/local/lib/R/site-library/RInside/lib -L/usr/lib -lhighgui -Wl,-rpath,/usr/lib
 *
 *
 * OLD:
 *
 * g++ -lcv -lcxcore -lhighgui -L(path to opencv library) kneeAngle.cpp -o kneeAngle
 *
 * for example: 
 * 
 * Ubuntu Hoary (8.04) with opencv 1.0
 * g++ -lcv -lcxcore -lhighgui -L/usr/local/lib kneeAngle.cpp -o kneeAngle
 * 
 * Ubuntu Intrepid (8.10) with opencv 1.1
 * g++ `pkg-config --cflags opencv` kneeAngle.cpp -o kneeAngle `pkg-config --libs opencv`
 *
 * command to run the file
 * ./kneeAngle "path to video file"
 *
 * if get lots of errors on playing like: 
 * "[swscaler @ 0x8d24260]No accelerated colorspace conversion found from yuv420p to bgr24."
 * execute:
 * ./kneeAngle "path to video file" 2> /dev/null
 * 
 */

/*
 * Installing OpenCV on Ubuntu
 *
 * http://dircweb.king.ac.uk/reason/opencv_cvs.php
 */



/*
 * TODO:
 * -imprimeixi en arxiu xy de cada punt (6 columnes)
 *  -implement convexity defects (opencv book p.259) at findKneePointBack
 *  solve the problem with the cvCopy(frame_copy,result);
 *  	on blackAndMarkers, minimumFrame is the marked or the expected?
 *  -study kalman on openCV book (not interesting)... si, aplicar kalman enlloc de pointInside(previous)
 *
 *  need to do subpixel calculations for: thetaABD and thetaRealFlex, because the pixel unit is too big for this calculations
 *  eg: upLegMarkedDist: 170; upLegMarkedDistMax: 171.... 
 *  	produces kneeZetaSide of 18,46 and maybe with a htKneeMarked of only 3px (peson is almos full extended) 
 *  	resulting ABD is 80.
 *  	with subpixel, maybe the upLegMarkedDist is 170.4 and the Max is 170.6 this has kneeZetaSide of 8.25, and with htkneeMarked: 3px, ABD is: 70
 *  	as seen, maybe the problem is in the 3px. maybe this is not ABD, is ROT EXT, and is normal at this extension
 *  	
 *  	---------
 *  	maybe, getting closer to the camera (z axis) affects too much and space need to be calibrated before. 
 *  	Maybe this means that we only can use this uncalibrated data for seen angle
 *  	---------
 *
 *  	 
 *
 *  calibration:
 *  -calibration and undistortion (distorsions: radial, and tangential)
 *     radial doesn't exist on image, because the line dividing floor and wall is straight from left to right (radial will make distort on places far from the center)
 *     tangential (p376,377) seems also to not exist on used camera, but best to record a cheesboard or square object to check
 *     maybe we can use opencv to paint the "claqueta" corners, and the use it as a chessboard, the problem is that this image is not always fully seen, but it doesn't need to be seen in all persons
 *     but use calibration all the time is not nice, because we prefer to record best the person, and zoom in or out if necessary. if the camera has not considerable distorsion, is best to don't need to be all the time with the chessboard. maybe we can (in the software) test the camera one time to see its distorsion. A 398 diu que és millor que es vegi el chessboard des de diferents llocs, sinó la solució no serà bona. Així que millor oblidar-se de pintar la claqueta com a chessboard quan entra per la dreta. Pero sí que es podria calcular la distorsió de la càmera i provar un mateix salt (amb marcadors) amb undistort i sense. Val a dir també que els punts cercats de la imatge no són a les cantonades, així que la radial distort afectarà poc; està per veure la tangencial, tot i que és més cosa de cheap cameres.
 */

#include "opencv/cv.h"
#include "opencv/highgui.h"
//#include <opencv/cxcore.h>
#include "stdio.h"
#include "math.h"
//#include<iostream>
//#include<fstream>
//#include<vector>
#include <string>

#include <RInside.h>

#include "kneeAngleGlobal.cpp"
#include "kneeAngleUtil.cpp"
#include "kneeAngleFunctions.cpp"
#include "kneeAngleRInside.cpp"


//using namespace std;

int menu(IplImage *, CvFont);
void readOptions();

	
int main(int argc,char **argv)
{
	if(argc < 2)
	{
		char *startMessage = new char[300];
		sprintf(startMessage, "\nkneeAngle HELP.\n\nProvide file location as a first argument...\nOptional: as 2nd argument provide a fraction of video to start at that frame, or a concrete frame.\nOptional: as 3rd argument provide mode you want to execute (avoiding main menu).\n\t%d: validation; %d: blackWithoutMarkers; %d: skinOnlyMarkers.\n\nEg: Start at frame 5375:\n\tkneeAngle myfile.mov 5375\nEg:start at 80 percent of video and directly as blackWithoutMarkers:\n\tkneeAngle myFile.mov .8 %d\n\nNote another param can be used to default thresholdLargestContour on validation and blackWithoutMarkers", 
				validation, blackWithoutMarkers, skinOnlyMarkers, blackWithoutMarkers);
		std::cout<< startMessage <<std::endl;
		exit(1);
	}

	CvCapture* capture = NULL;

	char * fileName = argv[1];
//	printf("%s", fileName);
	capture = cvCaptureFromAVI(fileName);
	if(!capture)
	{
		exit(0);
	}
	
	int framesNumber = cvGetCaptureProperty(capture, CV_CAP_PROP_FRAME_COUNT);

	if(argc >= 3) {
		if(atof(argv[2]) < 1) 			
			StartAt = framesNumber * atof(argv[2]);
		else 					
			StartAt = atoi(argv[2]);	//start at selected frame
	}
	
	printf("Number of frames: %d\t Start at:%d\n\n", framesNumber, StartAt);
	
	ProgramMode = undefined;
	if(argc >= 4) 
		ProgramMode = atoi(argv[3]);
	
	int threshold;
	//this is currently only used on validation and blackWithoutMarkers to have a threshold to find the contour
	//(different than threshold for three points)
	int thresholdLargestContour = -1; 
	int thresholdMax = 255;
	int thresholdInc = 1;
				
	if(argc == 5) 
		thresholdLargestContour = atoi(argv[4]);

	readOptions();
	printf("--- Options: ---\n");
	printf("ShowContour:%d\n", ShowContour);
	printf("Debug:%d\n", Debug);
	printf("UsePrediction:%d\n", UsePrediction);
	printf("PlayDelay:%d\n", PlayDelay);
	printf("PlayDelayFoundAngle:%d\n", PlayDelayFoundAngle);
	printf("ZoomScale:%f\n", ZoomScale);

	//3D
	//printf("framesCount;hip.x;hip.y;knee.x;knee.y;toe.x;toe.y;angle seen;angle side;angle real\n");
	//not 3D but record thresholds
	//printf("framesCount;hip.x;hip.y;knee.x;knee.y;toe.x;toe.y;angle current; threshold, th.hip; th.knee; th.toe\n");

	
	/* initialization variables */
	IplImage *frame=0,*frame_copy=0,*gray=0,*segmented=0,*edge=0,*temp=0,*output=0;
	IplImage *outputTemp=0, *frame_copyTemp=0;
	IplImage *segmentedValidationHoles=0;
	IplImage *foundHoles=0;
	IplImage *result=0;
	IplImage *resultStick=0;
	IplImage *extension=0, *extensionSeg=0, *extensionSegHoles=0;
	IplImage *gui=0; //interact with user
	
	CvFont font;
	int fontLineType = CV_AA; // change it to 8 to see non-antialiased graphics
	double fontSize = .4;
	cvInitFont(&font, CV_FONT_HERSHEY_COMPLEX, fontSize, fontSize, 0.0, 1, fontLineType);
	
	cvNamedWindow("gui",1);
	//if ProgramMode is not defined or is invalid, ask user
	if(ProgramMode < validation || ProgramMode > skinOnlyMarkers) {
		gui = cvLoadImage("kneeAngle_intro.png");
		cvShowImage("gui", gui);
		ProgramMode = menu(gui, font);
	}
	
	if(ProgramMode == skinOnlyMarkers) {
		UsingContour = false;
		gui = cvLoadImage("kneeAngle_skin.png");
	}
	else if(ProgramMode == validation) {
		UsingContour = true;
		gui = cvLoadImage("kneeAngle_black_contour.png");
	} 
	else
		gui = cvLoadImage("kneeAngle_black_without.png");

			
	imageGuiResult(gui, "Starting... please wait.", font);
	cvWaitKey(100); //to allow gui image be shown
	

	// ----------------------------- create fileNames -----------------------------

	/*
	   each line: hipX, hipY, kneeX, kneeY, toeX, toeY, angle, rectH, rectHP (rectHP means Percent)
	fDataRaw: non filtered non smoothed data
	fDatasmooth: filtered, smoothed data
	*/
	FILE *fDataRaw; 
	FILE *fDataSmooth;

	char extensionRaw[] = "_raw.csv";
	char extensionSmooth[] = "_smooth.csv";

	char fDataRawName [strlen(fileName) + strlen(extensionRaw)];
	char fDataSmoothName [strlen(fileName) + strlen(extensionSmooth)];
	
	strcpy(fDataRawName, fileName);
	changeExtension(fDataRawName, extensionRaw);

	strcpy(fDataSmoothName, fileName);
	changeExtension(fDataSmoothName, extensionSmooth);

	printf("\n--- files: ---\n");
	printf("video file:\n%s\n",fileName);
	printf("csv files:\n%s\n%s\n\n",fDataRawName, fDataSmoothName);

	if((fDataRaw=fopen(fDataRawName,"w"))==NULL){
		printf("Error, no se puede escribir en el fichero %s\n",fDataRawName);
		fclose(fDataRaw);
		exit(0);
	}
	fclose(fDataRaw);
	/*
	if((fDataSmooth=fopen(fDataSmoothName,"w"))==NULL){
		printf("Error, no se puede escribir en el fichero %s\n",fDataSmoothName);
		fclose(fDataSmooth);
		exit(0);
	}
	fclose(fDataSmooth);
	*/
	
	// ----------------------------- create windows -----------------------------
	
	if (ProgramMode == blackWithoutMarkers)
		cvNamedWindow("result",1);
	else 
		cvNamedWindow("threshold",1);

	// ----------------------------- define vars -------------------------------------------
	
	int kneeMinWidth = 0;

	int kneeWidthAtExtension = 0;
	int toeExtensionWidth = 0;
	
	bool foundAngle = false; //found angle on current frame
	bool foundAngleOneTime = false; //found angle at least one time on the video

	double knee2Hip,knee2Toe,hip2Toe;
	double thetaExpected, thetaMarked;
	//string text,angle;
	std::string text;
	double minThetaExpected = 360;
	double minThetaMarked = 360;
	double minThetaRealFlex = 360;
	char buffer[15];
					
	bool askForMaxFlexion = false; //false: no ask (means no auto end before jump)

	int kneePointWidth = -1;
	int toePointWidth = -1;
		
	//to make lines at resultPointsLines
	CvPoint notFoundPoint;
	notFoundPoint.x = 0; notFoundPoint.y = 0;
	int lowestAngleFrame = 0;
	int lowestAngleFrameReally = 0; //related to framesCountReally


	char *label = new char[150];
	
	bool shouldEnd = false;

	//stick storage
	CvMemStorage* stickStorage = cvCreateMemStorage(0);
	CvSeq* hipSeq = cvCreateSeq( CV_SEQ_KIND_GENERIC|CV_32SC2, sizeof(CvSeq), sizeof(CvPoint), stickStorage );
	CvSeq* kneeSeq = cvCreateSeq( CV_SEQ_KIND_GENERIC|CV_32SC2, sizeof(CvSeq), sizeof(CvPoint), stickStorage );
	CvSeq* toeSeq = cvCreateSeq( CV_SEQ_KIND_GENERIC|CV_32SC2, sizeof(CvSeq), sizeof(CvPoint), stickStorage );
  
	double avgThetaDiff = 0;
	double avgThetaDiffPercent = 0;
	double avgKneeDistance = 0;
	double avgKneeBackDistance = 0;
	int framesDetected = 0;
	int framesCount = 0;
	int framesCountReally = 0; //do not trust on capture info. This is a counter on iterations
	//show a counting message every n frames:
	int framesCountShowMessage = 0;
	int framesCountShowMessageAt = 100;

	//to advance fast and really fast
	bool forward = false;
	int forwardSpeed = 50;
	bool fastForward= false; 
	int fastForwardSpeed = 200;
	int forwardCount = 1;
	bool backward = false;
	int backwardSpeed = 50;
	
	bool jumping = false;


	bool labelsAtLeft = true;
		
	CvPoint hipMarked = pointToZero();
	CvPoint kneeMarked = pointToZero();
	CvPoint toeMarked = pointToZero();
	CvPoint hipOld = pointToZero();
	CvPoint kneeOld = pointToZero();
	CvPoint toeOld = pointToZero();
	//used for pressing 'l': last.  when a point is lost
	CvPoint hipOldWorked = pointToZero();
	CvPoint kneeOldWorked = pointToZero();
	CvPoint toeOldWorked = pointToZero();
	
	CvSeq* seqPredicted;
	CvPoint hipPredicted = pointToZero();
	CvPoint kneePredicted = pointToZero();
	CvPoint toePredicted = pointToZero();

	//here data of all frames is stored
	std::vector<int> hipXVector;
	std::vector<int> hipYVector;
	std::vector<int> kneeXVector;
	std::vector<int> kneeYVector;
	std::vector<int> toeXVector;
	std::vector<int> toeYVector;
	std::vector<double> angleVector;
	std::vector<int> rectHVector; //Height
	std::vector<double> rectHWVector; //Height/Width
			
	std::vector<int> kneePointFrontXVector;
	std::vector<double> kneePointFrontYVector;
	std::vector<int> kneePointBackXVector;
	std::vector<double> kneePointBackYVector;
	
	/*
	int upLegMarkedDist = 0;
	int upLegMarkedDistMax = 0;
	int downLegMarkedDist = 0;
	int downLegMarkedDistMax = 0;
	*/

	IplImage* imgZoom;
	
	//threshold for the three specific points
	int thresholdROIH = -1;
	int thresholdROIK = -1;
	int thresholdROIT = -1;
	
	//to store threshold at min angle
	int thresholdAtMinAngle;
	int thresholdROIHAtMinAngle = -1;
	int thresholdROIKAtMinAngle = -1;
	int thresholdROITAtMinAngle = -1;
			
	//this helps at ROI to determine the area where threhold will be changed
	//useful if we have a white marker surrounded by white pant
	//then we can have main threshold low
	//then we can have main threshold ROI on that point high
	//then we can have main threshold ROI size low (butif marker moves a lot, then we will need to increase a bit)
	int thresholdROISizeH = 16; 
	int thresholdROISizeK = 16; 
	int thresholdROISizeT = 16;
	int thresholdROISizeInc = 2; //increment on each pulsation
	int thresholdROISizeMin = 8;

	int key;
			
	//used to convert Y of OpenCV (top) to Y of R (bottom)
	int verticalHeight;


	//ProgramMode == validation
	bool extensionDoIt = true;
	bool extensionCopyDone = false;
	CvPoint kneeMarkedAtExtension = pointToZero();
	CvPoint hipMarkedAtExtension = pointToZero();
	CvPoint hipPointBackAtExtension = pointToZero();

	//this contains data useful to validation: max and min Height and Width of all rectangles
	int validationRectHMax = 0;
	/*
	int validationRectHMin = 100000;
	int validationRectWMax = 0;
	int validationRectWMin = 100000;
	//angle at min Height of validation rectangle
	double validationRectHMinThetaMarked = 180;
	*/

	CvRect maxrect;

	MouseClicked = undefined;	
	cvSetMouseCallback( "gui", on_mouse_gui, 0 );
		
	bool reloadFrame = false;
	int forcePause = false;
			
	bool storeResultImage = false;

	// ---------------------- Kalman filter (unused) --------------------------
	/*
	//CvKalman* kalman = cvCreateKalman(2,1,0);
	CvKalman* kalman = cvCreateKalman(2,2,0);
	CvMat* state = cvCreateMat(2,1,CV_32FC1);
	CvMat* process_noise = cvCreateMat(2,1,CV_32FC1);
	//CvMat* measurement = cvCreateMat(1,1,CV_32FC1);
	CvMat* measurement = cvCreateMat(2,1,CV_32FC1);
	CvPoint measurement_pt;
	cvZero( measurement );
	const float F[] = { 1, 1, 0, 1};
	memcpy(kalman->transition_matrix->data.fl, F, sizeof(F));

	CvRandState rng;
	cvRandInit(&rng,0,1,-1,CV_RAND_UNI);
	cvRandSetRange(&rng,0,0.1,0);
	rng.disttype = CV_RAND_NORMAL;
	cvRand(&rng, process_noise);			

	cvSetIdentity(kalman->measurement_matrix, 	cvRealScalar(1));
	cvSetIdentity(kalman->process_noise_cov, 	cvRealScalar(1e-5));
	cvSetIdentity(kalman->measurement_noise_cov, 	cvRealScalar(1e-1));
	cvSetIdentity(kalman->error_cov_post, 		cvRealScalar(1));

	CvPoint k0; k0.x=0; k0.y=0;
	//kalman->state_post = k0;
	//kalman->state_post.data.fl[0] = &k0;
	*/
	/* /kalman */


	while(!shouldEnd) 
	{
		/*
		 * 1
		 * GET FRAME AND FLOW CONTROL
		 */

		if( !reloadFrame)
			frame = cvQueryFrame(capture);
		reloadFrame = false;
		
		//when we go back, we doesn't always land where we want, this is safe:
		double capturedFrame = cvGetCaptureProperty( capture, CV_CAP_PROP_POS_FRAMES);
		framesCount = capturedFrame +1;
	
		if(!frame)
			break;
		if(StartAt > framesCount) {
			//show a counting message every framesCountShowMessageAt, and continue
			framesCountShowMessage ++;
			if(framesCountShowMessage >= framesCountShowMessageAt) {
				eraseGuiResult(gui, true);
				sprintf(label, "frame: %d... (%d%%)", framesCount, 100*framesCount/StartAt);
				imageGuiResult(gui, label, font);
				//printf("%s\n", label);
				cvWaitKey(25); //to allow gui image be shown
				framesCountShowMessage = 0;
			}
			continue;
		}
		if(forward || fastForward) {
			if(
					forward && (forwardCount < forwardSpeed) ||
					fastForward && (forwardCount < fastForwardSpeed)) {
				forwardCount ++;
				continue;
			} else {
				//end of forwarding
				forwardCount = 1;
				forward = false;
				fastForward = false;
				eraseGuiResult(gui, true);

				//mark that we are forwarding to the holesdetection
				//then it will not need to match a point with previous point
				//hipOld = pointToZero();
				//kneeOld = pointToZero();
				//toeOld = pointToZero();
			}
		}
		if(jumping || backward) {
				//hipOld = pointToZero();
				//kneeOld = pointToZero();
				//toeOld = pointToZero();
				jumping = false;
				backward = false;
				eraseGuiResult(gui, true);
		}

		framesCountReally ++;


		/* 
		 * 2
		 * CREATE IMAGES
		 */


		if( !frame_copy ) 
			frame_copy = cvCreateImage( cvSize(frame->width,frame->height),IPL_DEPTH_8U, frame->nChannels );
		
		if( frame->origin == IPL_ORIGIN_TL )
			cvCopy( frame, frame_copy, 0 );
		else
			cvFlip( frame, frame_copy, 0 );


		if(!gray)
		{
			gray = 		cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,1);
			segmented =	cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,1);
			segmentedValidationHoles  =	cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,1);
			foundHoles  =	cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,3);
			edge =	cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,1);
			temp = cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,1);
			output = cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,1);
			outputTemp = cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,1);
			result = cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,3);
			resultStick = cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,3);
			extension =	cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,3);
			extensionSeg =	cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,1);
			extensionSegHoles =	cvCreateImage(cvGetSize(frame),IPL_DEPTH_8U,1);

			if(ProgramMode == skinOnlyMarkers) {
				cvCvtColor(frame_copy,gray,CV_BGR2GRAY);
				threshold = calculateThresholdStart(gray, false);
			}
			else if(ProgramMode == validation) {
				cvCvtColor(frame_copy,gray,CV_BGR2GRAY);
				threshold = calculateThresholdStart(gray, false);
				if(thresholdLargestContour == -1)
					thresholdLargestContour = calculateThresholdStart(gray, true);
			}
			else {
				cvCvtColor(frame_copy,gray,CV_BGR2GRAY);
				threshold = calculateThresholdStart(gray, true);
			}

			verticalHeight = cvGetSize(frame).height;
		}

		cvSmooth(frame_copy,frame_copy,2,5,5);
		cvCvtColor(frame_copy,gray,CV_BGR2GRAY);

		/*
		 * 3 
		 * FIND THREE MARKER POINTS
		 */


		//predict where will be the points now
		if(UsePrediction) {
			seqPredicted = predictPoints(
					hipXVector, hipYVector,
					kneeXVector, kneeYVector,
					toeXVector, toeYVector); 
			hipPredicted = *CV_GET_SEQ_ELEM( CvPoint, seqPredicted, 0); 
			kneePredicted = *CV_GET_SEQ_ELEM( CvPoint, seqPredicted, 1); 
			toePredicted = *CV_GET_SEQ_ELEM( CvPoint, seqPredicted, 2); 
		} else {
			hipPredicted = hipOld;
			kneePredicted = kneeOld;
			toePredicted = toeOld;
		}
	
		//uncomment to show how prediction works
		/*
		if(UsePrediction) {
			printf("1 OLD:  %d;%d;%d;%d;%d;%d\n", hipOld.x, hipOld.y, 
					kneeOld.x, kneeOld.y, toeOld.x, toeOld.y);
			printf("2 PRED: %d;%d;%d;%d;%d;%d\n", hipPredicted.x, hipPredicted.y, 
					kneePredicted.x, kneePredicted.y, toePredicted.x, toePredicted.y);
		}
		*/


		if(ProgramMode == skinOnlyMarkers || ProgramMode == validation) 
		{

			/* kalman */
			/*
			const CvMat* prediction = cvKalmanPredict(kalman, 0);
			CvPoint prediction_pt = cvPoint(
					cvRound(prediction->data.fl[0]), 
					cvRound(prediction->data.fl[1]));
					*/
			/* /kalman */
	


			cvCvtColor(frame_copy,output,CV_BGR2GRAY);
			cvCvtColor(frame_copy,outputTemp,CV_BGR2GRAY);
			cvThreshold(gray, output, threshold, thresholdMax,CV_THRESH_BINARY_INV);


			if(thresholdROIH != -1 || thresholdROIK != -1 || thresholdROIT != -1)  
			{
				if(thresholdROIH != -1) {
					output = changeROIThreshold(gray, output, hipMarked, thresholdROIH, thresholdMax, thresholdROISizeH);
				}
				if(thresholdROIK != -1) {
					output = changeROIThreshold(gray, output, kneeMarked, thresholdROIK, thresholdMax, thresholdROISizeK);
				} 
				if(thresholdROIT != -1) {
					output = changeROIThreshold(gray, output, toeMarked, thresholdROIT, thresholdMax, thresholdROISizeT);
				}
			}


			//this segmented is to find the three holes
			cvThreshold(gray,segmented,threshold,thresholdMax,CV_THRESH_BINARY_INV);

			CvSeq* seqHolesEnd;

			if(ProgramMode == skinOnlyMarkers) {
				seqHolesEnd = findHolesSkin(output, frame_copy, 
						hipMarked, kneeMarked, toeMarked, hipPredicted, kneePredicted, toePredicted, font);
			}
			else {
				//this segmented is to find the contour (threshold is lot little)
				cvThreshold(gray,segmentedValidationHoles,thresholdLargestContour,thresholdMax,CV_THRESH_BINARY_INV);
				cvThreshold(gray,segmented,thresholdLargestContour,thresholdMax,CV_THRESH_BINARY_INV);

				//maxrect = findLargestContour(segmented, output, ShowContour);
				maxrect = findLargestContour(segmented, outputTemp, ShowContour);
							
				//search in output all the black places (pants) and 
				//see if there's a hole in that pixel on segmentedValidationHoles
				//but firsts do a copy because maybe it doesn't work
				if( !frame_copyTemp ) 
					frame_copyTemp = cvCreateImage( cvSize(frame->width,frame->height),IPL_DEPTH_8U, frame->nChannels );
				
				cvCopy(frame_copy,frame_copyTemp);


				if(thresholdROIH != -1) {
					segmented = changeROIThreshold(gray, segmented, hipMarked, 
							thresholdROIH, thresholdMax, thresholdROISizeH);
					segmentedValidationHoles = changeROIThreshold(gray, segmentedValidationHoles, hipMarked, 
							thresholdROIH, thresholdMax, thresholdROISizeH);
				}
				if(thresholdROIK != -1) {
					segmented = changeROIThreshold(gray, segmented, kneeMarked, 
							thresholdROIK, thresholdMax, thresholdROISizeK);
					segmentedValidationHoles = changeROIThreshold(gray, segmentedValidationHoles, kneeMarked, 
							thresholdROIK, thresholdMax, thresholdROISizeK);
				}
				if(thresholdROIT != -1) {
					segmented = changeROIThreshold(gray, segmented, toeMarked, 
							thresholdROIT, thresholdMax, thresholdROISizeT);
					segmentedValidationHoles = changeROIThreshold(gray, segmentedValidationHoles, toeMarked, 
							thresholdROIT, thresholdMax, thresholdROISizeT);
				}

	
				seqHolesEnd = findHoles(
						outputTemp, segmented, foundHoles, frame_copy,  
						maxrect, hipOld, kneeOld, toeOld, hipPredicted, kneePredicted, toePredicted, font);

				//if hip or toe is touching a border of the image
				//then will not be included in largest contour
				//then use findHolesSkin to find points
				CvPoint myHip = pointToZero();
				CvPoint myKnee = pointToZero();
				CvPoint myToe = pointToZero();
				myHip = *CV_GET_SEQ_ELEM( CvPoint, seqHolesEnd, 0); 
				myKnee = *CV_GET_SEQ_ELEM( CvPoint, seqHolesEnd, 1); 
				myToe = *CV_GET_SEQ_ELEM( CvPoint, seqHolesEnd, 2 ); 

				//can change to skin related if has problems with the contour
				if( ! pointIsNull(myHip) && ! pointIsNull(myKnee) && ! pointIsNull(myToe) ) {
					cvCopy(segmentedValidationHoles, output);
					if(! UsingContour) {
						UsingContour = true;
						gui = cvLoadImage("kneeAngle_black_contour.png");
					}
				}
				else {
					UsingContour = false;
					gui = cvLoadImage("kneeAngle_black.png");
					cvCopy(frame_copyTemp,frame_copy);

					//--------------------------------- testing stuff ---------------------
					cvShowImage("threshold",output); //is this testing?
					//cvShowImage("toClick", frame_copy);
					
					imageGuiResult(gui, "going", font); //is this testing?

					//printf("threshold :%d\n", threshold);
					//printf("thresholdLC :%d\n", thresholdLargestContour);
					//cvWaitKey(500); //to allow messages be shown
					//--------------------------------- end of testing --------------------

					seqHolesEnd = findHolesSkin(output, frame_copy, 
							hipMarked, kneeMarked, toeMarked, hipPredicted, kneePredicted, toePredicted, font);

					imageGuiResult(gui, "returned", font);
					//cvWaitKey(500); //to allow gui image be shown
				}
			}
			cvShowImage("threshold", output);


			hipMarked = *CV_GET_SEQ_ELEM( CvPoint, seqHolesEnd, 0); 
			kneeMarked = *CV_GET_SEQ_ELEM( CvPoint, seqHolesEnd, 1 ); 
			toeMarked = *CV_GET_SEQ_ELEM( CvPoint, seqHolesEnd, 2 ); 
		

			//if all the points are ok, the dump in pointsDump file to smooth and predict
			if( ! pointIsNull(hipMarked) && ! pointIsNull(kneeMarked) && ! pointIsNull(toeMarked) ) {
				hipXVector.push_back(hipMarked.x);
				hipYVector.push_back(hipMarked.y);
				kneeXVector.push_back(kneeMarked.x);
				kneeYVector.push_back(kneeMarked.y);
				toeXVector.push_back(toeMarked.x);
				toeYVector.push_back(toeMarked.y);
			}


			//-----------------  kalman filter unused --------------------------
			/*
			measurement_pt = kneeMarked;

			//cvMatMulAdd(kalman->measurement_matrix, x_k,z_k,z_k);

			//crossPoint(frame_copy, cvPoint(measurement_pt.x -20, measurement_pt.y), YELLOW, BIG); //works
			//crossPoint(frame_copy, cvPoint(prediction_pt.x +20, prediction_pt.y), WHITE, BIG); //0,0
			// /kalman 

			cvNamedWindow( "toClick", 1 );
			cvShowImage("toClick", frame_copy);

			cvKalmanCorrect(kalman, measurement);

			cvRandSetRange(&rng,0,sqrt(kalman->process_noise_cov->data.fl[0]),0);
			cvRand(&rng, process_noise);			
			cvMatMulAdd(kalman->transition_matrix, measurement, process_noise, measurement);
			*/
			

			crossPoint(frame_copy, hipMarked, GREY, MID);
			crossPoint(frame_copy, kneeMarked, GREY, MID);
			crossPoint(frame_copy, toeMarked, GREY, MID);

			cvNamedWindow( "toClick", 1 );
			cvShowImage("toClick", frame_copy);


			//if frame before nothing was detected (maybe first frame or after a forward or jump
			//OR a point in this frame was not detected
			if(
					(pointIsNull(hipOld) && pointIsNull(kneeOld) && pointIsNull(toeOld)) ||
					(pointIsNull(hipMarked) || pointIsNull(kneeMarked) || pointIsNull(toeMarked)) ) 
			{
				// force a playPause and reload frame after
				// then all the code of key mouse interaction will be together at end of loop
				forcePause = true;
				reloadFrame = true;
			}

		} 
			
					
		//store old points that worked
		if(!pointIsNull(hipOld))
			hipOldWorked = hipOld;
		if(!pointIsNull(kneeOld))
			kneeOldWorked = kneeOld;
		if(!pointIsNull(toeOld))
			toeOldWorked = toeOld;

		hipOld = hipMarked;
		kneeOld = kneeMarked;
		toeOld = toeMarked;

		
		/*
		 * 4
		 * PRINT MARKERS RELATED INFO AND DO CALCULATIONS LIKE ANGLE
		 */


		if(ProgramMode == skinOnlyMarkers || ProgramMode == validation) 
		{
			if(pointIsNull(hipMarked) || pointIsNull(kneeMarked) || pointIsNull(toeMarked))
				thetaMarked = -1;
			else {
				thetaMarked = findAngle2D(hipMarked, toeMarked, kneeMarked);
				
				//store minThetaMarked if not marked to reload (bad detection, or first frame)
				if(!reloadFrame && thetaMarked < minThetaMarked) {
					minThetaMarked = thetaMarked;
					cvCopy(frame_copy,result);
					storeResultImage = true;
					lowestAngleFrame = framesCount;
					lowestAngleFrameReally = framesCountReally;

					//store thresholds	
					thresholdAtMinAngle = threshold;
					thresholdROIHAtMinAngle = thresholdROIH;
					thresholdROIKAtMinAngle = thresholdROIK;
					thresholdROITAtMinAngle = thresholdROIT;
				}


				if(ProgramMode != skinOnlyMarkers) {
					cvRectangle(frame_copy,
							cvPoint(maxrect.x,maxrect.y),
							cvPoint(maxrect.x + maxrect.width, maxrect.y + maxrect.height),
							CV_RGB(255,0,0),1,1);

					//assign validationRect data if maxs or mins reached
					if(maxrect.height > validationRectHMax)
						validationRectHMax = maxrect.height;
					/*
					if(maxrect.height < validationRectHMin) {
						validationRectHMin = maxrect.height;
						//store angle at validationRectHMin
						//and see how differs from minimum angle
						validationRectHMinThetaMarked = thetaMarked;
					}

					if(maxrect.width > validationRectWMax)
						validationRectWMax = maxrect.width;
					if(maxrect.width < validationRectWMin)
						validationRectWMin = maxrect.width;
						*/
	
				}




				//---------------------------- 3D angle calculations ------------------------
				/*
				 * NOT doing 3D calculations now
				
				 
				CvPoint HT;
				HT.y = kneeMarked.y;
				HT.x = hipMarked.x;

				
				 upLegMarkedDist = getDistance(hipMarked, kneeMarked);
				if(upLegMarkedDist > upLegMarkedDistMax)
					upLegMarkedDistMax = upLegMarkedDist;
				downLegMarkedDist = getDistance(toeMarked, kneeMarked);
				if(downLegMarkedDist > downLegMarkedDistMax)
					downLegMarkedDistMax = downLegMarkedDist;
				
					double kneeZetaSide = sqrt( pow(upLegMarkedDistMax,2) - pow(upLegMarkedDist,2) );
				double htKneeMarked = getDistance (HT, kneeMarked);

				double thetaABD = (180.0/M_PI)*atan( (double) kneeZetaSide / htKneeMarked );

				double thetaRealFlex = findAngle3D(hipMarked, toeMarked, kneeMarked, 0, 0, -kneeZetaSide);
				if(thetaRealFlex < minThetaRealFlex) 
					minThetaRealFlex = thetaRealFlex;


				if(ProgramMode == skinOnlyMarkers) {
					printOnScreen(output, font, CV_RGB(0,0,0), labelsAtLeft,
							framesCount, threshold, 
							(double) upLegMarkedDist *100 /upLegMarkedDistMax, 
							(double) downLegMarkedDist *100 /downLegMarkedDistMax,
							thetaMarked, minThetaMarked,
							thetaABD, thetaRealFlex, minThetaRealFlex
						     );
					cvShowImage("toClick", frame_copy);
					cvShowImage("threshold",output);

					printf("%d;%d;%d;%d;%d;%d;%d;%.2f;%.2f;%.2f %d;%d %.2f;%.2f\n", 
							framesCount, 
							hipMarked.x, frame->height - hipMarked.y,
							kneeMarked.x, frame-> height - kneeMarked.y,
							toeMarked.x, frame->height - toeMarked.y,
							thetaMarked, thetaABD, thetaRealFlex,
							upLegMarkedDist, upLegMarkedDistMax, 
							kneeZetaSide, htKneeMarked);
				}

				printOnScreen(frame_copy, font, CV_RGB(255,255,255), labelsAtLeft,
						framesCount, threshold, 
						(double) upLegMarkedDist *100 /upLegMarkedDistMax, 
						(double) downLegMarkedDist *100 /downLegMarkedDistMax,
						thetaMarked, minThetaMarked,
						thetaABD, thetaRealFlex, minThetaRealFlex
					     );
				 */
			
				/*	
				printf("%d;%d;%d;%d;%d;%d;%d;%.2f;%d;%d;%d;%d\n", 
						framesCount, 
						hipMarked.x, frame->height - hipMarked.y,
						kneeMarked.x, frame-> height - kneeMarked.y,
						toeMarked.x, frame->height - toeMarked.y,
						thetaMarked,
						threshold, thresholdROIH, thresholdROIK, thresholdROIT
				      );
				      */
				//---------------------------- end of 3D angle calculations ---------------------
				
				printOnScreen(frame_copy, font, CV_RGB(255,255,255), labelsAtLeft,
						framesCount, 
						hipMarked.x, frame->height - hipMarked.y,
						kneeMarked.x, frame-> height - kneeMarked.y,
						toeMarked.x, frame->height - toeMarked.y,
						thetaMarked, minThetaMarked,
						threshold, thresholdROIH, thresholdROIK, thresholdROIT,
						thresholdROISizeH, thresholdROISizeK, thresholdROISizeT,
						thresholdLargestContour, UsingContour);
			
				//this stores image with above printOnScreen data	
				if(storeResultImage) {
					cvCopy(frame_copy,result);
					storeResultImage = false;
				}


				/*
				   if( (ProgramMode == validation || ProgramMode == blackWithoutMarkers)
				   && foundAngle) {
				   */
				/*
				//print data
				double thetaSup = findAngle2D(hipExpected, cvPoint(0,kneeExpected.y), kneeExpected);
				double thetaMarkedSup = findAngle2D(hipMarked, cvPoint(0, kneeMarked.y), kneeMarked);

				double thetaInf = findAngle2D(cvPoint(0,kneeExpected.y), toeExpected, kneeExpected);
				double thetaMarkedInf = findAngle2D(cvPoint(0,kneeMarked.y), toeMarked, kneeMarked);

				printf("%7d %7.2f %7.2f [%7.2f %7.2f] %7.2f %7.2f %7.2f [%7.2f %7.2f] [%7.2f] [%7.2f]\n", framesCount, thetaExpected, thetaMarked, 
				thetaMarked-thetaExpected, relError(thetaExpected, thetaMarked), 
				getDistance(kneeExpected, kneeMarked), 
				thetaSup-thetaMarkedSup, thetaInf-thetaMarkedInf
				, getDistance(kneeMarked, hipMarked), getDistance(kneeExpected, hipExpected)
				, getDistance(kneeExpected, hipPointBack)
				, getDistance(kneePointBack, hipPointBack)
				);

				avgThetaDiff += abs(thetaMarked-thetaExpected);
				avgThetaDiffPercent += abs(relError(thetaExpected, thetaMarked));
				avgKneeDistance += getDistance(kneePoint, kneeMarked);
				*/
				/*
				   framesDetected ++;
				   }
				   */


				/*
				   if(ProgramMode == validation || ProgramMode == blackWithoutMarkers)
				   cvShowImage("result",frame_copy);
				   */


				//Finds the minimum angle between Hip to Knee line and Knee to Toe line
				/*
				if(thetaRealFlex == minThetaRealFlex) {
					cvCopy(frame_copy,result);
					lowestAngleFrame = framesCount;
				}
				*/
				
				//cvShowImage("threshold",output);
				
				//exit if we are going up and soon jumping.
				//toe will be lost
				//detected if minThetaMarked is littler than thetaMarked, when thetaMarked is big
				if(askForMaxFlexion && thetaMarked > 140 && minThetaMarked +10 < thetaMarked)
				{
					cvShowImage("toClick", frame_copy);
					imageGuiResult(gui, "Min flex before. End?. 'y'es, 'n'o, 'N'ever", font);
					int option = optionAccept(true);	
					eraseGuiResult(gui, true);
					if(option==YES) {
						printf("\ntm: %f, mtm: %f, frame: %d\n", thetaMarked, minThetaMarked, framesCount);
						shouldEnd = true;
					} else if(option==NEVER)
						askForMaxFlexion = false;
				}
			}
			cvShowImage("threshold",output);
		}
				   

//		if(ProgramMode == validation || ProgramMode == blackWithoutMarkers)
//      			cvShowImage("result",frame_copy);


		CvPoint hipExpected;
		CvPoint kneeExpected;
		CvPoint toeExpected;

		/*
		 * 5 a
		 * IF BLACKANDMARKERS MODE, FIND POINTS
		 * UNUSED NOW BECAUSE WE ARE USING ONLY RECTANGLE
		 * AND KNEEPOINTS
		 */

		/*
		if(ProgramMode == validation || ProgramMode == blackWithoutMarkers) 
		{
			CvPoint hipPointBack;
			CvPoint kneePointBack;
			CvPoint kneePointFront;

//			cvWaitKey(0);  aqui:

			hipPointBack = findHipPoint(output,maxrect);

//			cvWaitKey(0);  abans

			//provisionally ubicate hipPoint at horizontal 1/2
			hipExpected.x = hipPointBack.x + (findWidth(output, hipPointBack, true) /2);
			hipExpected.y = hipPointBack.y;

			//knee	
			kneePointFront = findKneePointFront(output, maxrect, foundAngleOneTime);
			//hueco popliteo
			kneePointBack = findKneePointBack(output, maxrect, kneePointFront.x, foundAngleOneTime); 

			//toe
			int toeMinWidth;
			if(kneeWidthAtExtension == 0)
				toeMinWidth = findWidth(output, kneePointFront, false) / 2;
			else
				toeMinWidth = kneeWidthAtExtension / 2;
			CvPoint toeExpected = findToePoint(output,maxrect,kneePointFront.x,kneePointFront.y, toeMinWidth);
			
			
			crossPoint(frame_copy, hipPointBack, RED, MID);
			crossPoint(frame_copy, hipExpected, BLUE, MID);
			crossPoint(frame_copy, kneePointFront, GREY, MID);
			crossPoint(frame_copy, kneePointBack, GREY, MID);
			crossPoint(frame_copy, toeExpected, GREEN, MID);
			cvShowImage("result",frame_copy);


			foundAngle = false;
			if(kneeMinWidth == 0)
				kneeMinWidth = kneePointFront.x - hipPointBack.x;
			else {
				if((double)(kneePointFront.x- hipPointBack.x) > 1.15*kneeMinWidth && //or 1.05 or 1.15
						upperSimilarThanLower(hipExpected, kneePointFront, toeExpected)
				  )
						//this is for validation
						//&& !pointIsNull(hipMarked) && !pointIsNull(kneeMarked) && 
						//!pointIsNull(toeMarked))
				{
					if(foundAngleOneTime) {
						foundAngle = true;
					} 
					else if(extensionDoIt && extensionCopyDone) 
					{
						// 
						// first time, confirm we found knee ok (and angle)
						// maybe is too early
						//
						crossPoint(frame_copy, kneePointFront, GREY, MID);
						crossPoint(frame_copy, kneePointBack, GREY, MID);

						cvShowImage("result",frame_copy);
						imageGuiResult(gui, "knee found. Accept? 'n'o, 'y'es", font);
					
						int option = optionAccept(false);	
						eraseGuiResult(gui, true);

						if(option==YES) {
							printf("Accepted\n");
							foundAngle = true;
							foundAngleOneTime = true;

							CvRect rectExt = findKneeCenterAtExtension(extensionSeg, kneePointFront.y);
							
							//kneeRect height center will be in the middle of kneepointfront and back
							//the kneeRect.y should be (1/2 height below)
							rectExt.height = rectExt.width;
							rectExt.y = ( (kneePointFront.y + kneePointBack.y) /2 ) - (rectExt.height/2);

					
							//debug	
							//cvNamedWindow("Extension seg old",1);
							//cvShowImage("Extension seg old", extensionSeg);


							double kneeCenterExtX = rectExt.x + (double)(rectExt.width /2 );
							double kneeCenterExtY = rectExt.y + (double)(rectExt.height /2 );

							double kneeCenterExtXPercent = 100 * (double)(kneeCenterExtX - rectExt.x) / rectExt.width; //aprox 50%
							double kneeCenterExtYPercent = 100 * (double)(kneeCenterExtY - rectExt.y) / rectExt.height; //aprox 50%
							
							printf("kneeCenterExtX,Y: %.1f(%.1f%%) %.1f(%.1f%%)\n", 
									kneeCenterExtX, 
									kneeCenterExtXPercent, 
									kneeCenterExtY, 
									kneeCenterExtYPercent 
									);				//debug
				

							if(validation) {	
								//
								// now print differences between:
								// CvPoint(kneeCenterExtX, kneePointFront.y) and kneeMarkedAtEXtension
								//
								printf("marked at ext: x: %d, y: %d\n", kneeMarkedAtExtension.x, kneeMarkedAtExtension.y); //debug

								//see the % of rectangle where kneeMarked is (at extension)
								double kneeMarkedXPercent = 100 * (double)(kneeMarkedAtExtension.x - rectExt.x) / rectExt.width;
								double kneeMarkedYPercent = 100 * (double)(kneeMarkedAtExtension.y - rectExt.y) / rectExt.height;
							
								cvLine(extension,
										kneeMarkedAtExtension,
										cvPoint(kneeCenterExtX, kneeCenterExtY),
										WHITE,1,1);
								
								printf("knee diff: x: %.1f (%.1f%%), y: %.1f (%.1f%%)\n", 
										kneeMarkedAtExtension.x - kneeCenterExtX,
										kneeMarkedXPercent - kneeCenterExtXPercent,
										kneeMarkedAtExtension.y - kneeCenterExtY,
										kneeMarkedYPercent - kneeCenterExtYPercent);	


								//hip
								double hipMarkedX = hipMarkedAtExtension.x - kneeCenterExtX;
								double hipMarkedXPercent = 100 * (double) hipMarkedX / rectExt.width;
								printf("hip diff: x: %.1f (%.1f%%)\n", 
										hipMarkedX,
										hipMarkedXPercent
								      );
								cvLine(extension,
										hipMarkedAtExtension,
										cvPoint(kneeCenterExtX, hipMarkedAtExtension.y),
										CV_RGB(128,128,128),1,1);
							}
							
							cvRectangle(extension, 
									cvPoint(rectExt.x, rectExt.y), 
									cvPoint(rectExt.x + rectExt.width, rectExt.y + rectExt.height),
									MAGENTA,1,8,0
									);
							crossPoint(extension,cvPoint(rectExt.x + rectExt.width, kneePointFront.y), WHITE, MID);
							crossPoint(extension,cvPoint(rectExt.x, kneePointBack.y), WHITE, MID);

							
							//back
							double backDistance = kneeCenterExtX - hipPointBackAtExtension.x;
							double backDistancePercent = 100 * (double) backDistance / rectExt.width ;
							printf("back width: %.1f (%.1f%%)\n", 
									backDistance, 
									backDistancePercent
									);
							cvLine(extension,
									hipPointBackAtExtension,
									cvPoint(kneeCenterExtX, hipPointBackAtExtension.y),
									CV_RGB(128,128,128),1,1);
							
							showScaledImage(extension, "Extension frame");
						} else {
							foundAngle = false;
							
							if(option==FORWARD) {
								forward = true;
								printf("forwarding ...\n");
							} else if(option==FASTFORWARD) {
								fastForward = true;
								printf("fastforwarding ...\n");
							} else if(option==BACKWARD) {
								backward = true;

								//try to go to previous (backwardspeed) frame
								cvSetCaptureProperty( capture, CV_CAP_PROP_POS_FRAMES, 
										framesCount - backwardSpeed -1 );
									
								imageGuiResult(gui, "Backwarding...", font);
								continue;
							} else if(option==QUIT) {
								shouldEnd = true;
								printf("exiting ...\n");
							}
						}

						//if extensionDoIt == false, nothing of above is done
					}
				}
			}

			//Finds angle between Hip to Knee line and Knee to Toe line
			if(foundAngle)
			{
				// ------------ knee stuff ----------

				//find width of knee, only one time and will be used for all the photogrammes
				if(kneePointWidth == -1) 
					kneePointWidth = findWidth(output, kneePointFront, false);

				kneeExpected = kneePointInNearMiddleOfFrontAndBack(
						kneePointBack, kneePointFront, kneePointWidth, frame_copy);
				crossPoint(frame_copy, kneePointBack, GREY, MID);
				crossPoint(frame_copy, kneePointFront, GREY, MID);
				
				cvLine(frame_copy,kneePointFront,kneePointBack, GREY,1,1);
				crossPoint(frame_copy, kneeExpected, RED, BIG);


				int kneeWidth = kneePointFront.x - kneePointBack.x;
				int kneeHeight = kneeWidth; //kneeHeight can be 0, best to use kneeWidth for percent, is more stable
				int kneeHeightBoxDown = ( (kneePointFront.y + kneePointBack.y) /2 ) - (kneeHeight /2);
				
				cvRectangle(frame_copy, 
						cvPoint(kneePointBack.x, kneeHeightBoxDown), 
						cvPoint(kneePointBack.x + kneeWidth, kneeHeightBoxDown + kneeHeight),
						MAGENTA,1,8,0);

				if(validation) {
					int kneeMarkedPosX = kneeWidth - (kneeMarked.x - kneePointBack.x);
					double kneeMarkedPercentX = (double) kneeMarkedPosX * 100 / kneeWidth;
					int kneeMarkedPosY = kneeHeight - (kneeMarked.y - kneeHeightBoxDown);
					double kneeMarkedPercentY = (double) kneeMarkedPosY * 100 / kneeHeight;
					
				}

				//
				//if(pointIsNull(hipMarked) || pointIsNull(kneeMarked) || pointIsNull(toeMarked))
				//	thetaMarked = -1;
				//else
				//	thetaMarked = findAngle2D(hipMarked, toeMarked, kneeMarked);
				//	

				// ------------ toe stuff ----------

				//
				// don't find width of toe for each photogramme
				// do it only at first, because if at any photogramme, as flexion advances, 
				// knee pointfront is an isolate point at the right of the lowest part of the pants
				// because the back part of kneepoint has gone up
				//

				//if(toePointWidth == -1) 
					toePointWidth = findWidth(output, toeExpected, false);
				crossPoint(frame_copy, toeExpected, GREY, MID);

				thetaExpected = findAngle2D(hipExpected, toeExpected, kneeExpected);
				
				
				//printf("%d;%.2f;%.2f;%.2f\n", framesCount, thetaMarked, kneeMarkedPercentX, kneeMarkedPercentY);
				double angleBack =findAngle2D(hipPointBack, cvPoint(toeExpected.x - toePointWidth, toeExpected.y), kneePointBack); 
				//printf("%d;%.2f;%.2f;%.2f\n", framesCount, angleBack, kneeMarkedPercentX, kneeMarkedPercentY);
				cvLine(frame_copy,hipPointBack, kneePointBack,CV_RGB(255,0,0),1,1);
				cvLine(frame_copy,kneePointBack, cvPoint(toeExpected.x - toePointWidth, toeExpected.y),CV_RGB(255,0,0),1,1);


				//fix toeExpected.x at the 1/2 of the toe width
				//depending on kneeAngle
				toeExpected.x = fixToePointX(toeExpected.x, toePointWidth, thetaExpected);
				crossPoint(frame_copy, toeExpected, RED, BIG);


				// ------------ hip stuff ----------

				//fix hipExpected ...
				crossPoint(frame_copy, hipPointBack, GREY, MID);

				//... find at 3/2 of hip (surely under the hand) ...
				//thetaExpected = findAngle2D(hipExpected, toeExpected, kneeExpected);
				hipExpected = fixHipPoint1(output, hipPointBack.y, kneeExpected, thetaExpected);
				crossPoint(frame_copy, hipExpected, GREY, MID);

				//... cross first hippoint with the knee-hippoint line to find real hippoint
				hipExpected = fixHipPoint2(output, hipPointBack.y, kneeExpected, hipExpected);
				crossPoint(frame_copy, hipExpected, RED, BIG);


				// ------------ flexion angle ----------

				//find flexion angle
				thetaExpected = findAngle2D(hipExpected, toeExpected, kneeExpected);
				//double thetaBack = findAngle2D(hipExpected, toeExpected, kneePointBack);

				//draw 2 main lines
				cvLine(frame_copy,kneeExpected,hipExpected,CV_RGB(255,0,0),1,1);
				cvLine(frame_copy,kneeExpected,toeExpected,CV_RGB(255,0,0),1,1);

				cvSeqPush( hipSeq, &hipExpected );
				cvSeqPush( kneeSeq, &kneeExpected );
				cvSeqPush( toeSeq, &toeExpected );


				if(thetaExpected < minThetaExpected)
				{
					minThetaExpected = thetaExpected;
					cvCopy(frame_copy,result);
					lowestAngleFrame = framesCount;
				}
			}
			else {
				//angle not found
				cvSeqPush( hipSeq, &notFoundPoint );
				cvSeqPush( kneeSeq, &notFoundPoint );
				cvSeqPush( toeSeq, &notFoundPoint );

				//
				// if we never have found the angle, 
				// and this maxrect is wider than previous maxrect (flexion started)
				// then capture previous image to process knee at extension search
				//
				if(extensionDoIt && ! foundAngleOneTime && ! extensionCopyDone) {
					cvShowImage("result",frame_copy);
					imageGuiResult(gui, "Extension copy. Accept? 'n'o, 'y'es", font);
					int option = optionAccept(false);
					eraseGuiResult(gui, true);

					if(option==YES) {
						cvCopy(frame_copy, extension);
						
						//cvCopy(segmented, extensionSeg);
						cvCopy(output, extensionSeg);

						//cvCopy(segmentedValidationHoles, extensionSegHoles);
						//printf("\nhere: x: %d, y: %d\n", kneeMarked.x, kneeMarked.y);
						if(validation) {
							kneeMarkedAtExtension = kneeMarked;
							hipMarkedAtExtension = hipMarked;
						}
							
						hipPointBackAtExtension = hipPointBack;

						kneeWidthAtExtension = findWidth(output, kneePointFront, false);

						showScaledImage(extension, "Extension frame");
						
						extensionCopyDone = true;
					} else {
						if(option==FORWARD) {
							forward = true;
							printf("forwarding ...\n");
						} else if(option==FASTFORWARD) {
							fastForward = true;
							printf("fastforwarding ...\n");
						} else if(option==BACKWARD) {
							backward = true;
							
							//try to go to previous (backwardspeed) frame
							cvSetCaptureProperty( capture, CV_CAP_PROP_POS_FRAMES, 
									framesCount - backwardSpeed -1 );
							
							imageGuiResult(gui, "Backwarding...", font);
							continue;
						} else if(option==QUIT) {
							shouldEnd = true;
							printf("exiting ...\n");
						}
					}
				}
			}
				
			cvShowImage("result",frame_copy);
		}
		*/
		
		/*
		 * 5 b
		 * IF BLACKANDMARKERS MODE, FIND RECTANGLE, AND KNEE POINTS
		 */


		if( ! pointIsNull(hipMarked) && ! pointIsNull(kneeMarked) && ! pointIsNull(toeMarked) ) {
			angleVector.push_back(thetaMarked);

			if(ProgramMode == skinOnlyMarkers) {
				rectHVector.push_back(-1);
				rectHWVector.push_back(-1);
				kneePointFrontXVector.push_back(-1);
				kneePointFrontYVector.push_back(-1);
				kneePointBackXVector.push_back(-1); 
				kneePointBackYVector.push_back(-1); 
			} else {
				rectHVector.push_back(maxrect.height);
				rectHWVector.push_back((double) maxrect.height / maxrect.width);

				//knee visible points on black pants
				CvPoint kneePointFront = findKneePointFront(outputTemp, maxrect, validationRectHMax);
				CvPoint kneePointBack = findKneePointBack(outputTemp, maxrect, kneePointFront.x, validationRectHMax); 
				crossPoint(frame_copy, kneePointFront, GREY, MID);
				crossPoint(frame_copy, kneePointBack, GREY, MID);
			
				//if kneePointFront is not detected, it's 0.
				//don't convert when it's undetected
				//we need to know at which % of maxrect it's kneePointFront
				//same for KneePointBack
				kneePointFrontXVector.push_back(kneePointFront.x);
				double myKPFY = kneePointFront.y;
				if(myKPFY != 0) 
					myKPFY = 100 - (100 * (double) (kneePointFront.y - maxrect.y) / maxrect.height);
				kneePointFrontYVector.push_back(myKPFY);

				kneePointBackXVector.push_back(kneePointBack.x); 
				double myKPBY = kneePointBack.y;
				if(myKPBY != 0) 
					myKPBY = 100 - (100 * (double) (kneePointBack.y - maxrect.y) / maxrect.height);
				kneePointBackYVector.push_back(myKPBY);
			}
		}
			
		//cvCopy(frame_copy,result);
		cvShowImage("toClick", frame_copy);


		//cvWaitKey(0);

		/* 
		 * 6
		 * WAIT FOR KEYS OR MANAGE MOUSE INTERACTION
		 */

		int myDelay = PlayDelay;
		if(foundAngle)
			myDelay = PlayDelayFoundAngle;

		key = (char) cvWaitKey(myDelay);

		//printf("mc: %d ", MouseClicked);  

		if(MouseClicked == quit || key == 27 || key == 'q') // 'ESC'
			shouldEnd = true;

		if (MouseClicked == FORWARD) { 
			forward = true;
			imageGuiResult(gui, "Forwarding...", font);
			cvWaitKey(50); //to print above message
		} else if (MouseClicked == FASTFORWARD) { 
			fastForward = true;
			imageGuiResult(gui, "FastForwarding...", font);
			cvWaitKey(50); //to print above message
		} else if (MouseClicked == BACKWARD) {
			backward = true;

			//try to go to previous (backwardspeed) frame
			cvSetCaptureProperty( capture, CV_CAP_PROP_POS_FRAMES, 
					framesCount - backwardSpeed -1 );

			imageGuiResult(gui, "Backwarding...", font);
			cvWaitKey(50); //to print above message
		//} else if (key == 'j' || key == 'J') {
			/*
			//jump frames
			printf("current frame: %d. Jump to: ", framesCount);
			int jump;
			scanf("%d", &jump);

			//works with opencv1.1!!
			//now we can go back and forth
			cvSetCaptureProperty( capture, CV_CAP_PROP_POS_FRAMES, jump-1 );

			framesCount = jump;

			//hlps to know when we jumped and we have to initialize hipOld, kneeOld, toOld
			jumping = true;
			printf("jumping ...\n");
			imageGuiResult(gui, "Jumping...", font);
			*/
		//}
		} 
		//threshold of large contour
		else if( MouseClicked == TCONTOURMORE || MouseClicked == TCONTOURLESS) 
			forcePause = true;
		else if(MouseClicked == BACKTOCONTOUR) 
			forcePause = true;
		//thresholds of points
		else if(MouseClicked == TGLOBALMORE || MouseClicked == TGLOBALLESS ||
			MouseClicked == THIPMORE || MouseClicked == THIPLESS ||
			MouseClicked == TKNEEMORE || MouseClicked == TKNEELESS ||
			MouseClicked == TTOEMORE || MouseClicked == TTOELESS ||
			MouseClicked == TGLOBALMORE || MouseClicked == TGLOBALLESS ||
			MouseClicked == SHIPMORE || MouseClicked == SHIPLESS ||
			MouseClicked == SKNEEMORE || MouseClicked == SKNEELESS ||
			MouseClicked == STOEMORE || MouseClicked == STOELESS)
		{
			//if a threshold button is pushed, force a pause
			forcePause = true;
		}
		else if(key == 'v') {
			forcePause = true;
			MouseClicked = MINIMUMFRAMEVIEW;
		}
		else if(key == 'd') {
			forcePause = true;
			MouseClicked = MINIMUMFRAMEDELETE;
		}
		
		//pause is also forced on skin if a marker is not ok
		if (MouseClicked == PLAYPAUSE || key == 'p' || forcePause) 
		{
			if(!forcePause)
				MouseClicked = UNDEFINED;  
			if(key == 'p')
				key = NULL;

			forcePause = false;

			imageGuiResult(gui, "Paused.", font);
			//int thresholdROIChanged = TOGGLENOTHING;
			bool thresholdROIChanged = false;
					
			int mult;

			bool done = false;
			do {
				key = (char) cvWaitKey(50);
				//cvWaitKey(50);
			
				if(MouseMultiplier)
					mult = 10;
				else
					mult = 1;

				if (MouseClicked == PLAYPAUSE || key == 'p') 
					done = true;
		
				else if(MouseClicked == quit || key == 27 || key == 'q') {
					done = true;
					shouldEnd = true;
				}
				
				else if(MouseClicked == BACKWARD) { 
					backward = true;
					//try to go to previous (backwardspeed) frame
					cvSetCaptureProperty( capture, CV_CAP_PROP_POS_FRAMES, 
						framesCount - backwardSpeed -1 );

					imageGuiResult(gui, "Backwarding...", font);
					done = true;
				}
			
				else if(MouseClicked == FORWARDONE) { 
					forward = true;
					forwardCount = forwardSpeed -1; //this makes advance only one frame
					imageGuiResult(gui, "Forward one", font);
					done = true;
				}

				else if(MouseClicked == FORWARD) { 
					forward = true;
					imageGuiResult(gui, "Forwarding...", font);
					done = true;
				}

				else if(MouseClicked == FASTFORWARD) { 
					fastForward = true;
					imageGuiResult(gui, "FastForwarding...", font);
					done = true;
				}
				
				else if(MouseClicked == MINIMUMFRAMEVIEW || key == 'v') {
					cvShowImage("Minimum Frame", result);
					imageGuiResult(gui, "Shown minimum frame. Paused.", font);
				}
				else if(MouseClicked == MINIMUMFRAMEDELETE || key == 'd') { 
					minThetaMarked = 360;
					cvShowImage("Minimum Frame", result);
					imageGuiResult(gui, "Deleted stored angle. Paused.", font);
				}
		
				
				else if(MouseClicked == ZOOM || key == 'z') {
					if(Zoomed) {
						eraseGuiMark(gui, ZOOM);
						cvDestroyWindow("Zoomed");
						cvReleaseImage(&imgZoom);
						Zoomed = false;
						cvSetMouseCallback( "toClick", on_mouse_mark_point, 0 );
					} else {
						toggleGuiMark(gui, ZOOM);
						imgZoom = zoomImage(frame_copy);
						cvNamedWindow( "Zoomed", 1 );
						cvShowImage("Zoomed", imgZoom);
						Zoomed = true;
						cvSetMouseCallback( "Zoomed", on_mouse_mark_point, 0 );
					}
					MouseClicked = UNDEFINED;  
				}

				else if(MouseClicked == THIPMORE || MouseClicked == THIPLESS) {
					if(pointIsNull(hipMarked)) {
						//force mark first
						MouseClicked = HIPMARK;
					} else {
						if(thresholdROIH == -1) //undefined
							thresholdROIH = threshold;

						if(MouseClicked == THIPMORE) 
							thresholdROIH += thresholdInc * mult;
						else {
							thresholdROIH -= thresholdInc * mult;
							if(thresholdROIH < 0)
								thresholdROIH = 0;
						}
						thresholdROIChanged = true;
						MouseClicked = UNDEFINED;  
					}
				}

				else if(MouseClicked == TKNEEMORE || MouseClicked == TKNEELESS) {
					if(pointIsNull(kneeMarked)) {
						//force mark first
						MouseClicked = KNEEMARK;
					} else {
						if(thresholdROIK == -1) //undefined
							thresholdROIK = threshold;

						if(MouseClicked == TKNEEMORE) 
							thresholdROIK += thresholdInc * mult;
						else {
							thresholdROIK -= thresholdInc * mult;
							if(thresholdROIK < 0)
								thresholdROIK = 0;
						}
						thresholdROIChanged = true;
						MouseClicked = UNDEFINED;  
					}
				}

				else if(MouseClicked == TTOEMORE || MouseClicked == TTOELESS) {
					if(pointIsNull(toeMarked)) {
						//force mark first
						MouseClicked = TOEMARK;
					} else {
						if(thresholdROIT == -1) //undefined
							thresholdROIT = threshold;

						if(MouseClicked == TTOEMORE) 
							thresholdROIT += thresholdInc * mult;
						else {
							thresholdROIT -= thresholdInc * mult;
							if(thresholdROIT < 0)
								thresholdROIT = 0;
						}
						thresholdROIChanged = true;
						MouseClicked = UNDEFINED;  
					}
				}

				else if(MouseClicked == SHIPMORE || MouseClicked == SHIPLESS) {
					if(pointIsNull(hipMarked)) {
						//force mark first
						MouseClicked = HIPMARK;
					} else {
						if(MouseClicked == SHIPMORE) 
							thresholdROISizeH += thresholdROISizeInc;
						else {
							thresholdROISizeH -= thresholdROISizeInc;
							if(thresholdROISizeH < thresholdROISizeMin)
								thresholdROISizeH = thresholdROISizeMin;
						}
						thresholdROIChanged = true;
						MouseClicked = UNDEFINED;  
					}
				}
				
				else if(MouseClicked == SKNEEMORE || MouseClicked == SKNEELESS) {
					if(pointIsNull(kneeMarked)) {
						//force mark first
						MouseClicked = KNEEMARK;
					} else {
						if(MouseClicked == SKNEEMORE) 
							thresholdROISizeK += thresholdROISizeInc;
						else {
							thresholdROISizeK -= thresholdROISizeInc;
							if(thresholdROISizeK < thresholdROISizeMin)
								thresholdROISizeK = thresholdROISizeMin;
						}
						thresholdROIChanged = true;
						MouseClicked = UNDEFINED;  
					}
				}
				
				else if(MouseClicked == STOEMORE || MouseClicked == STOELESS) {
					if(pointIsNull(toeMarked)) {
						//force mark first
						MouseClicked = TOEMARK;
					} else {
						if(MouseClicked == STOEMORE) 
							thresholdROISizeT += thresholdROISizeInc;
						else {
							thresholdROISizeT -= thresholdROISizeInc;
							if(thresholdROISizeT < thresholdROISizeMin)
								thresholdROISizeT = thresholdROISizeMin;
						}
						thresholdROIChanged = true;
						MouseClicked = UNDEFINED;  
					}
				}
		
				//if we are in blackOnlyMarkers but we cannot find three points in contour
				//then we are in UsingContour = false
				//a mode like skinOnlyMarkers, but will return to contour if find points on contour next frame
				//or if userwanted to play with threshold:
				else if(MouseClicked == BACKTOCONTOUR) {
					UsingContour = true;
					gui = cvLoadImage("kneeAngle_black_contour.png");
					cvShowImage("gui", gui);
					MouseClicked = UNDEFINED;  

					cvThreshold(gray,segmentedValidationHoles,thresholdLargestContour,thresholdMax,CV_THRESH_BINARY_INV);
					cvThreshold(gray,segmented,thresholdLargestContour,thresholdMax,CV_THRESH_BINARY_INV);

					maxrect = findLargestContour(segmented, outputTemp, ShowContour);
					
					//predict where will be the points now
					if(UsePrediction) {
						seqPredicted = predictPoints(
								hipXVector, hipYVector,
								kneeXVector, kneeYVector,
								toeXVector, toeYVector); 
						hipPredicted = *CV_GET_SEQ_ELEM( CvPoint, seqPredicted, 0); 
						kneePredicted = *CV_GET_SEQ_ELEM( CvPoint, seqPredicted, 1); 
						toePredicted = *CV_GET_SEQ_ELEM( CvPoint, seqPredicted, 2); 
					} else {
						hipPredicted = hipOld;
						kneePredicted = kneeOld;
						toePredicted = toeOld;
					}

					
					findHoles(
							outputTemp, segmented, foundHoles, frame_copyTemp,  
							maxrect, hipOld, kneeOld, toeOld, hipPredicted, kneePredicted, toePredicted, font);

					cvCopy(segmentedValidationHoles, output);
					cvShowImage("threshold", output);
				}

				else if( MouseClicked == TCONTOURMORE || MouseClicked == TCONTOURLESS) {
					if(MouseClicked == TCONTOURMORE)
						thresholdLargestContour ++;
					else
						thresholdLargestContour --;
						
					eraseGuiResult(gui, true);
					sprintf(label, "Threshold: %d", thresholdLargestContour);
					imageGuiResult(gui, label, font);

					cvThreshold(gray,segmentedValidationHoles,thresholdLargestContour,thresholdMax,CV_THRESH_BINARY_INV);
					cvCopy(segmentedValidationHoles, output);
					cvShowImage("threshold", output);
					MouseClicked = UNDEFINED;  
				}
				
				else if (MouseClicked == TGLOBALMORE || MouseClicked == TGLOBALLESS || thresholdROIChanged) {  
					if (MouseClicked == TGLOBALMORE)  
						threshold += thresholdInc * mult;
					else if (MouseClicked == TGLOBALLESS) {
						threshold -= thresholdInc * mult;
						if(threshold < 0)
							threshold = 0;
					}
					MouseClicked = UNDEFINED;  
					MouseMultiplier = false;
		
					if(ProgramMode == skinOnlyMarkers || ProgramMode == validation) {
						sprintf(label, "Threshold: %d (%d,%d,%d) (%d,%d,%d)", 
								threshold, 
								thresholdROIH, thresholdROIK, thresholdROIT, 
								thresholdROISizeH, thresholdROISizeK, thresholdROISizeT);
						imageGuiResult(gui, label, font);

						cvThreshold(gray, output, threshold, thresholdMax,CV_THRESH_BINARY_INV);

						if(thresholdROIH != -1) 
							output = changeROIThreshold(gray, output, hipMarked, 
									thresholdROIH, thresholdMax, thresholdROISizeH);
						if(thresholdROIK != -1)
							output = changeROIThreshold(gray, output, kneeMarked, 
									thresholdROIK, thresholdMax, thresholdROISizeK);
						if(thresholdROIT != -1)
							output= changeROIThreshold(gray, output, toeMarked, 
									thresholdROIT, thresholdMax, thresholdROISizeT);

						cvShowImage("threshold", output);
					}
					/*
					else {		
						cvThreshold(gray,segmentedValidationHoles, threshold, thresholdMax,CV_THRESH_BINARY_INV);
						//create the largest contour image (stored on temp)
						cvThreshold(gray,segmented,threshold,thresholdMax,CV_THRESH_BINARY_INV);

						if(thresholdROIH != -1 || thresholdROIK != -1 || thresholdROIT != -1) {
							if(thresholdROIH != -1) {
								segmented = changeROIThreshold(gray, segmented, hipMarked, 
										thresholdROIH, thresholdMax, thresholdROISizeH);
								segmentedValidationHoles = 
									changeROIThreshold(gray, segmentedValidationHoles, hipMarked, 
											thresholdROIH, thresholdMax, thresholdROISizeH);
							}
							if(thresholdROIK != -1) {
								segmented = changeROIThreshold(gray, segmented, kneeMarked, 
										thresholdROIK, thresholdMax, thresholdROISizeK);
								segmentedValidationHoles = 
									changeROIThreshold(gray, segmentedValidationHoles, kneeMarked, 
											thresholdROIK, thresholdMax, thresholdROISizeK);
							}
							if(thresholdROIT != -1) {
								segmented = changeROIThreshold(gray, segmented, toeMarked, 
										thresholdROIT, thresholdMax, thresholdROISizeT);
								segmentedValidationHoles = 
									changeROIThreshold(gray, segmentedValidationHoles, toeMarked, 
											thresholdROIT, thresholdMax, thresholdROISizeT);
							}
							cvCopy(segmentedValidationHoles, output);
							cvShowImage("threshold", output);
						}

						maxrect = findLargestContour(segmented, output, ShowContour);

						//if(validation)
						//	updateHolesWin(segmentedValidationHoles);
						
						sprintf(label, "threshold: %d", threshold, thresholdROIH);
						imageGuiResult(gui, label, font);
					}
					*/
						
					thresholdROIChanged = false;
				}
					
				//remark bad found or unfound markers	
				if(
						MouseClicked == HIPMARK || key == 'h' ||
						MouseClicked == KNEEMARK || key == 'k' ||
						MouseClicked == TOEMARK || key == 't'
						) {
						
					int myMark = TOGGLENOTHING ;
					const char * Id = "";
					CvPoint markedBefore;
					
					if(MouseClicked == HIPMARK || key == 'h' ) {
						myMark = TOGGLEHIP;
						markedBefore = hipMarked;
						MarkedMouse = hipMarked;
						Id = "H";
						imageGuiResult(gui, "Mark Hip on toClick window. Or 'l': last", font);
					} else if(MouseClicked == KNEEMARK || key == 'k' ) {
						myMark = TOGGLEKNEE;
						markedBefore = kneeMarked;
						MarkedMouse = kneeMarked;
						Id = "K";
						imageGuiResult(gui, "Mark Knee on toClick window. Or 'l': last", font);
					} else {
						myMark = TOGGLETOE;
						markedBefore = toeMarked;
						MarkedMouse = toeMarked;
						Id = "T";
						imageGuiResult(gui, "Mark Toe on toClick window. Or 'l': last", font);
					}
						
					toggleGuiMark(gui, myMark);
					ForceMouseMark = myMark;
			
					cvSetMouseCallback( "toClick", on_mouse_mark_point, 0 );
					MouseClicked = UNDEFINED;  
					bool doneMarking = false;
					bool cancelled = false;
					do {
						key = (char) cvWaitKey(PlayDelay);
						if(!pointIsEqual(markedBefore, MarkedMouse)) {
							crossPoint(frame_copy, MarkedMouse, MAGENTA, BIG);
							imagePrint(frame_copy, cvPoint(MarkedMouse.x -20, MarkedMouse.y), Id, font, MAGENTA);
							cvShowImage("toClick", frame_copy);
							if(Zoomed) {
								crossPoint(imgZoom, cvPoint(ZoomScale * MarkedMouse.x, ZoomScale * MarkedMouse.y), MAGENTA, BIG);
								imagePrint(imgZoom, cvPoint(ZoomScale * (MarkedMouse.x -20), ZoomScale * MarkedMouse.y), Id, font, MAGENTA);
								cvShowImage("Zoomed", imgZoom);
							}
							doneMarking = true;
						}
				
						else if(MouseClicked == ZOOM || key == 'z') {
							if(Zoomed) {
								eraseGuiMark(gui, ZOOM);
								cvDestroyWindow("Zoomed");
								cvReleaseImage(&imgZoom);
								Zoomed = false;
								cvSetMouseCallback( "toClick", on_mouse_mark_point, 0 );
							} else {
								toggleGuiMark(gui, ZOOM);
								imgZoom = zoomImage(frame_copy);
								cvNamedWindow( "Zoomed", 1 );
								cvShowImage("Zoomed", imgZoom);
								Zoomed = true;
								cvSetMouseCallback( "Zoomed", on_mouse_mark_point, 0 );
							}
							MouseClicked = UNDEFINED;  
						}
						//if user want to put mark on last point it was (and then play with threshold)
						else if(key == 'l') {
							if(myMark == TOGGLEHIP) 
								MarkedMouse = hipOldWorked;
							if(myMark == TOGGLEKNEE) 
								MarkedMouse = kneeOldWorked;
							if(myMark == TOGGLETOE) 
								MarkedMouse = toeOldWorked;
							imageGuiResult(gui, "Last marked. Now adjust threshold", font);
							cvWaitKey(500); //to print above message
						}
					
						else if(
								MouseClicked == HIPMARK || key == 'h' ||
								MouseClicked == KNEEMARK || key == 'k' ||
								MouseClicked == TOEMARK || key == 't'
						       ) {
							cancelled = true;
						}
					} while(!doneMarking && !cancelled);
					
					eraseGuiMark(gui, myMark);
					
					bool unZoomAfterClick = true;
					if(unZoomAfterClick && Zoomed) {
						eraseGuiMark(gui, ZOOM);
						cvDestroyWindow("Zoomed");
						cvReleaseImage(&imgZoom);
						Zoomed = false;
						cvSetMouseCallback( "toClick", on_mouse_mark_point, 0 );
					}
					
					if(!cancelled) {
						if(myMark == TOGGLEHIP) {
							hipMarked = MarkedMouse;
							hipOld = MarkedMouse;
						} else if(myMark == TOGGLEKNEE) {
							kneeMarked = MarkedMouse;
							kneeOld = MarkedMouse;
						} else { 
							toeMarked = MarkedMouse;
							toeOld = MarkedMouse;
						}
					}

					eraseGuiResult(gui, true);
					ForceMouseMark = TOGGLENOTHING;
					MouseClicked = UNDEFINED;  
				}

			} while (! done);
					
			eraseGuiResult(gui, true);
				
		}

//		if(ProgramMode == validation) 
//			updateHolesWin(segmentedValidationHoles);
		
		
		MouseClicked = UNDEFINED;  

	}

	/*
	 * END OF MAIN LOOP
	 */
		
	imageGuiResult(gui, "Press 'q' to exit.", font);

	//if( (ProgramMode == validation || ProgramMode == blackWithoutMarkers) && foundAngleOneTime) 
	if(ProgramMode == validation || ProgramMode == blackWithoutMarkers) 
	{
		//relative to geometric points:
		//avgThetaDiff = (double) avgThetaDiff / framesDetected;
		//avgThetaDiffPercent = (double) avgThetaDiffPercent / framesDetected;
		//avgKneeDistance = (double) avgKneeDistance / framesDetected;
			
		cvNamedWindow("Minimum Frame",1);
		cvShowImage("Minimum Frame", result);

		/*
		printf("Minimum Frame\n");
		sprintf(label, "minblack minholes    diff diff(%%)");
		sprintf(label, "%8.2f %8.2f [%7.2f %7.2f]", minThetaExpected, minThetaMarked, 
				minThetaMarked-minThetaExpected, relError(minThetaExpected, minThetaMarked));
		printf("%s\n" ,label);
		*/
	}
	else {
		//printf("*** Result ***\nMin angle: %.2f, lowest angle frame: %d\n", minThetaMarked, lowestAngleFrame);
		cvNamedWindow("Minimum Frame",1);
		cvShowImage("Minimum Frame", result);
		printf("MIN: %d;%.2f\n", lowestAngleFrame, minThetaMarked);
	}
	

	do {
		key =  (char) cvWaitKey(0);
	} while (key != 'q' && key != 'Q');

	
	//start of flexion is the
	//last position of smoothed (and filtered) max vector size
	//on blackWithoutMarkers do it using rect height,
	//but on the rest, use hip marker because has less problems
	int flexionStartsAtFrame = 0;
	if(ProgramMode == blackWithoutMarkers)
		flexionStartsAtFrame = findLastPositionInVector(
				smoothVectorInt(rectHVector),
				findMaxInVector(smoothVectorInt(rectHVector), 0, rectHVector.size() -1)
				);
	else
		flexionStartsAtFrame = findLastPositionInVector(
				smoothVectorInt(hipYVector),
				findMinInVector(smoothVectorInt(hipYVector)) //min because here hipYVector has still 0 at the top
				);

	//---------------- write raw data file -------------------------------------
	if((fDataRaw=fopen(fDataRawName,"w"))==NULL){
		printf("Error, no se puede escribir en el fichero %s\n",fDataRawName);
	} else {
		//skinOnlyMarkers has no rect
		int rectHeightMax = -1;
		double rectHeightWidthMax = -1;
		if(ProgramMode != skinOnlyMarkers) {
			rectHeightMax = findMaxInVector(rectHVector, flexionStartsAtFrame, lowestAngleFrameReally);
			rectHeightWidthMax = findMaxInVector(rectHWVector, flexionStartsAtFrame, lowestAngleFrameReally);
		}

		fprintf(fDataRaw, "hipX;hipY;kneeX;kneeY;toeX;toeY;kpfX;kpfY;kpbX;kpbY;angle;rectH;rectHP;rectHW;rectHWP\n");
		for (int i=flexionStartsAtFrame; i < lowestAngleFrameReally; i ++) {
			double rectHeightPercent = -1;
			double rectHeightWidthPercent = -1;
			if(ProgramMode != skinOnlyMarkers) {
				rectHeightPercent = 100 * (double) rectHVector[i] / rectHeightMax;
				rectHeightWidthPercent = 100 * (double) rectHWVector[i] / rectHeightWidthMax;
			}

			//don't print when kneePointFront is not found, we need it
			//if(kneePointFrontYVector[i] > 0)
			//in this test we will print all the data, and then decide
				fprintf(fDataRaw, "%d;%d;%d;%d;%d;%d;%d;%f;%d;%f;%f;%d;%f;%f;%f\n", 
						hipXVector[i], verticalHeight - hipYVector[i],
						kneeXVector[i], verticalHeight - kneeYVector[i],
						toeXVector[i], verticalHeight - toeYVector[i],
						kneePointFrontXVector[i], kneePointFrontYVector[i], 
						kneePointBackXVector[i], kneePointBackYVector[i],
						angleVector[i], 
						rectHVector[i], rectHeightPercent,
						rectHWVector[i], rectHeightWidthPercent
				       );
		}
	}
	fclose(fDataRaw);

	//---------------- write smooth data file -------------------------------------
	/* currently unused OUTDATED: add rectHW*/
	/*
	if((fDataSmooth=fopen(fDataSmoothName,"w"))==NULL){
		printf("Error, no se puede escribir en el fichero %s\n",fDataSmoothName);
	} else {
		//smooth data
		hipXVector = smoothVectorInt(hipXVector);
		hipYVector = smoothVectorInt(hipYVector);
		kneeXVector = smoothVectorInt(kneeXVector);
		kneeYVector = smoothVectorInt(kneeYVector);
		toeXVector = smoothVectorInt(toeXVector);
		toeYVector = smoothVectorInt(toeYVector);
		rectHVector = smoothVectorInt(rectHVector);
		
		angleVector = smoothVectorDouble(angleVector);

		//skinOnlyMarkers has no rect
		int rectHeightMax = -1;
		if(ProgramMode != skinOnlyMarkers)
			rectHeightMax = findMaxInVector(rectHVector);

		fprintf(fDataSmooth, "hipX;hipY;kneeX;kneeY;toeX;toeY;kpfX;kpfY;kpbX;kpbY;angleTest;angle;rectH;rectHP\n");
		for (int i=flexionStartsAtFrame; i < lowestAngleFrameReally; i ++) {
			//Note: smoothed angle don't comes from smoothing the angle points, 
			//comes from calculating the angle in the smoothed X,Y of three joints
			CvPoint h;
			h.x = hipXVector[i]; h.y = hipYVector[i];
			CvPoint k;
			k.x = kneeXVector[i]; k.y = kneeYVector[i];
			CvPoint t;
			t.x = toeXVector[i]; t.y = toeYVector[i];
			double angleSmoothed = findAngle2D(h,t,k);
			
			double rectHeightPercent = -1;
			if(ProgramMode != skinOnlyMarkers)
				rectHeightPercent = 100 * (double) rectHVector[i] / rectHeightMax;

			//if kneePointFront is not detected, it's 0.
			//verticalHeight is used to convert Y of OpenCV (top) to Y of R (bottom)
			//don't convert to 384 when it's undetected
			//same for KneePointBack
			int myKPFY = kneePointFrontYVector[i];
			if(myKPFY != 0)
				myKPFY = verticalHeight - kneePointFrontYVector[i];
			int myKPBY = kneePointBackYVector[i];
			if(myKPBY != 0)
				myKPBY = verticalHeight - kneePointBackYVector[i];

			fprintf(fDataSmooth, "%d;%d;%d;%d;%d;%d;%d;%d;%d;%d;%f;%f;%d;%f\n", 
					hipXVector[i], verticalHeight - hipYVector[i],
					kneeXVector[i], verticalHeight - kneeYVector[i],
					toeXVector[i], verticalHeight - toeYVector[i],
					kneePointFrontXVector[i], myKPFY, 
					kneePointBackXVector[i], myKPBY,
					angleVector[i], //trying angle smoothed
					angleSmoothed, rectHVector[i], 
					rectHeightPercent);
		}
	}
	fclose(fDataSmooth);
	*/

	cvWaitKey(1500); //allow to end writing

	//------------------ clear memory ----------------------
	cvClearMemStorage( stickStorage );

	cvDestroyAllWindows();
	cvReleaseImage(&frame_copy);
	cvReleaseImage(&gray);
	cvReleaseImage(&segmented);
	cvReleaseImage(&segmentedValidationHoles);
	cvReleaseImage(&foundHoles);
	cvReleaseImage(&edge);
	cvReleaseImage(&temp);
	cvReleaseImage(&output);
	cvReleaseImage(&result);
//	if(!mixStickWithMinAngleWindow)
		cvReleaseImage(&resultStick);
}


int menu(IplImage * gui, CvFont font) 
{
	/* initial menu */

	int row = 1;
	int step = 16;
	int key = NULL;

	cvSetMouseCallback( "gui", on_mouse_gui_menu, 0 );

	do {
		key = (char) cvWaitKey(100);
		switch ( key ) {
			case 27: MouseClicked = quit; 			break; //27: ESC
			case 'q': MouseClicked = quit; 			break;
			case '1': MouseClicked = validation; 		break;
			case '2': MouseClicked = blackWithoutMarkers; 	break;
			case '3': MouseClicked = skinOnlyMarkers; 	break;
		}
	} while (MouseClicked == undefined);

	if(MouseClicked == quit)
		exit(0);

	eraseGuiWindow(gui);

	return MouseClicked;
}

void readOptions() {
	FILE *fOptions; 
	char fOptionsName[] = "kneeAngleOptions.txt";
	if((fOptions=fopen(fOptionsName,"r"))==NULL){
		printf("Error, no se puede leer el fichero %s\n",fOptionsName);
		fclose(fOptions);
		exit(0);
	}
	
	char option[50];
	char value[10];

	int endChar;
	bool fileEnd = false;
	while(!fileEnd) {
//		fscanf(fOptions,"%s;%s\n", option, value);
		//fscanf(fOptions,"%[^;]%[^\n]", option, value);
		fscanf(fOptions,"%[^;]", option);
		fgetc(fOptions); //gets the ';'
		fscanf(fOptions,"%[^\n]", value);
		fgetc(fOptions); //gets the '\n'

		if(strcmp(option,"ShowContour") ==0) {
			if(strcmp(stringToUpper(value),"TRUE") ==0) 
				ShowContour = true;
			else
				ShowContour = false;
		}
		else if(strcmp(option,"Debug") == 0) {
			if(strcmp(stringToUpper(value),"TRUE") ==0) 
				Debug = true;
			else 
				Debug = false;
		}
		else if(strcmp(option,"UsePrediction") == 0) {
			if(strcmp(stringToUpper(value),"TRUE") ==0)
				UsePrediction = true;
			else
				UsePrediction = false;
		}
		else if(strcmp(option,"PlayDelay") ==0)
			PlayDelay = atoi(value);
		else if(strcmp(option,"PlayDelayFoundAngle") ==0)
			PlayDelayFoundAngle = atoi(value);
		else if(strcmp(option,"ZoomScale") ==0)
			ZoomScale = atof(value);

		endChar = getc(fOptions);
		if(endChar == EOF)
			fileEnd = true;
		else
			ungetc(endChar, fOptions);
	}

}
				
