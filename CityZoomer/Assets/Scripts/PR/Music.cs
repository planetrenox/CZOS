using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace PR
{
    public class Music : MonoBehaviour
    {
        private AudioSource musicAudioSource;
        public AudioClip[] musicAudioList; // 31
        private int songCount;
        private TMP_Text songDisplayText;
        private bool isInRooftop;

        private void Awake()
        {
            musicAudioSource = GetComponent<AudioSource>();
            songDisplayText = GameObject.Find("Text_Music").GetComponent<TMP_Text>();
        }

        private void Start()
        {
            songCount = musicAudioList.Length;
            InvokeRepeating(nameof(musicPlay), 5, 10f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                nextSong();
            }
        }

        private void musicPlay()
        {
            if (musicAudioSource.isPlaying == false)
            {
                var song = musicAudioList[Random.Range(0, songCount)];
                musicAudioSource.PlayOneShot(song);
                displaySongName(song.name);
            }
            else
            {
                clearSongDisplay();
            }
        }

        public void nextSong()
        {
            musicAudioSource.Stop();
            var song = musicAudioList[Random.Range(0, songCount)];
            musicAudioSource.PlayOneShot(song);
            displaySongName(song.name);
        }

        private void displaySongName(string name)
        {
            songDisplayText.text = name;
        }

        private void clearSongDisplay()
        {
            songDisplayText.text = null;
        }
    }
}