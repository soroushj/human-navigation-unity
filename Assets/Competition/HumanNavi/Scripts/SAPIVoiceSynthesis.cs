﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpeechLib;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SIGVerse.Common;

namespace SIGVerse.Competition.HumanNavigation
{
	public interface IPlaybackGuidanceMessageHandler : IEventSystemHandler
	{
		void OnPlaybackGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus);
	}

	public interface ISpeakGuidanceMessageHandler : IEventSystemHandler
	{
		void OnSpeakGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus);
	}

	//public interface ISpeakMessageHandler : IEventSystemHandler
	//{
	//	void OnSpeakMessage(string message, string displayType);
	//}

	public interface IStopSpeakingHandler : IEventSystemHandler
	{
		void OnStopSpeaking();
	}

	public class SAPIVoiceSynthesis : MonoBehaviour, IPlaybackGuidanceMessageHandler
	{
		[HeaderAttribute("SAPI")]
		public string language = "409";
		public string gender = "Female";

		[HeaderAttribute("Guidance message param")]
		public int maxCharcters = 400;

		private List<GameObject> notificationDestinations;

		private SpVoice voice;

		//private Text guidanceMessageText;

		bool isSpeaking;

		// Use this for initialization
		void Awake()
		{
			this.voice = new SpVoice
			{
				Volume = 100, // Volume (no xml)
				Rate = 0      //   Rate (no xml)
			};

			SpObjectTokenCategory tokenCat = new SpObjectTokenCategory();
			tokenCat.SetId(SpeechLib.SpeechStringConstants.SpeechCategoryVoices, false);

			// Select tokens by specifying a semicolon-separated attribute.
			//     (Language:409=English (United States)/411=Japanese, Gender:Male/Female ... )
			ISpeechObjectTokens tokens = tokenCat.EnumerateTokens("Language=" + this.language + "; Gender=" + this.gender, null);

			this.voice.Voice = tokens.Item(0);

			//this.scene_recorder = GameObject.Find(PlaybackSystemParam.WorldRecorderName);

			this.notificationDestinations = new List<GameObject>();

			this.notificationDestinations.Add(GameObject.FindObjectOfType<HumanNaviScoreManager>().gameObject);
			this.notificationDestinations.Add(GameObject.FindObjectOfType<GuidanceMessagePanelController>().gameObject);
			this.notificationDestinations.Add(GameObject.FindObjectOfType<HumanNaviPlaybackCommon>().gameObject);

			this.isSpeaking = false;
		}

		//public void Start()
		//{
		//}

		void Update()
		{
			if (this.isSpeaking && this.voice.Status.RunningState == SpeechRunState.SRSEDone)
			{
				foreach (GameObject destination in this.notificationDestinations)
				{
					ExecuteEvents.Execute<IStopSpeakingHandler>
					(
						target: destination,
						eventData: null,
						functor: (reciever, eventData) => reciever.OnStopSpeaking()
					);
				}

				this.isSpeaking = false;
			}
		}

		public bool SpeakMessage(string message, string displayType)
		{
			if (this.voice.Status.RunningState == SpeechRunState.SRSEIsSpeaking)
			{
				SIGVerseLogger.Info("Speech API: SpeechRunState.SRSEIsSpeaking");
				return false;
			}

			string truncatedMessage;

			if(message.Length > maxCharcters)
			{
				truncatedMessage = message.Substring(0, maxCharcters);
				SIGVerseLogger.Info("Length of guidance message is over 400 charcters.");
			}
			else
			{
				truncatedMessage = message;
			}

			this.voice.Speak(truncatedMessage, SpeechVoiceSpeakFlags.SVSFlagsAsync);

			//foreach (GameObject destination in this.notificationDestinations)
			//{
			//	ExecuteEvents.Execute<ISpeakMessageHandler>
			//	(
			//		target: destination,
			//		eventData: null,
			//		functor: (reciever, eventData) => reciever.OnSpeakMessage(truncatedMessage, displayType)
			//	);
			//}

			// For recording
			foreach (GameObject destination in this.notificationDestinations)
			{
				ExecuteEvents.Execute<ISpeakGuidanceMessageHandler>
				(
					target: destination,
					eventData: null,
					functor: (reciever, eventData) => reciever.OnSpeakGuidanceMessage(new GuidanceMessageStatus(message, displayType))
				);
			}

			this.isSpeaking = true;

			return true;
		}

		public void SetLanguage(string language, string gender)
		{
			if      (language == "English")  { language = "409"; }
			else if (language == "Japanese") { language = "411"; }
			else { return; }

			SpObjectTokenCategory tokenCat = new SpObjectTokenCategory();
			tokenCat.SetId(SpeechLib.SpeechStringConstants.SpeechCategoryVoices, false);
			ISpeechObjectTokens tokens = tokenCat.EnumerateTokens("Language=" + language + "; Gender=" + gender, null);

			this.voice.Voice = tokens.Item(0);
		}

		public bool IsSpeaking()
		{
			if (this.voice.Status.RunningState == SpeechRunState.SRSEIsSpeaking) return true;
			else return false;
		}

		public void OnReceiveROSHumanNaviGuidanceMessage(SIGVerse.RosBridge.human_navigation.HumanNaviGuidanceMsg guidanceMsg)
		{
			this.SpeakMessage(guidanceMsg.message, guidanceMsg.display_type);
		}

		public void OnPlaybackGuidanceMessage(GuidanceMessageStatus guidanceMessageStatus)
		{
			this.SpeakMessage(guidanceMessageStatus.Message, guidanceMessageStatus.DisplayType);
		}
	}
}
