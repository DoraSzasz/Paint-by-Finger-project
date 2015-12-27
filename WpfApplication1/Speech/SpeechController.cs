using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Speech.Recognition;

using Microsoft.Research.Kinect.Audio;
using System.Speech.AudioFormat;
using System.IO;
using System.Threading;
using System.Windows;

namespace WpfApplication1.Speech
{
    public class SpeechController : INUIController
    {

        SpeechRecognitionEngine SpeechEngine;
        KinectAudioSource kaSource;
        Stream kasSource;
        IList<SpeechAction> speechRoot;
        IList<SpeechAction> speechCurrentNodes;

        #region INUIController

        public void Initialize(Microsoft.Research.Kinect.Nui.Runtime r)
        {
            Thread th = new Thread(Initialize);
            th.SetApartmentState(ApartmentState.MTA);
            th.Start();
        }


        public void Initialize()
        {
            try
            {
                kaSource = new KinectAudioSource();
                kaSource.FeatureMode = true;
                kaSource.AutomaticGainControl = false; //Important to turn this off for speech recognition
                kaSource.SystemMode = SystemMode.OptibeamArrayOnly; //No AEC for this sample
            }
            catch (Exception)
            {
                kaSource = null;
            }

            RecognizerInfo ri = SpeechRecognitionEngine.InstalledRecognizers().FirstOrDefault();
            if (ri == null)
            {
                throw new InvalidOperationException("There are no speech recognition engines installed");
            }
            SpeechEngine = new SpeechRecognitionEngine(ri.Id);

            if (kaSource != null)
            {
                kasSource = kaSource.Start();
                SpeechEngine.SetInputToAudioStream(kasSource,
                    new SpeechAudioFormatInfo(
                    EncodingFormat.Pcm, 16000, 16, 1,
                    32000, 2, null));
            }
            else
            {
                SpeechEngine.SetInputToDefaultAudioDevice();
            }

            SpeechEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sre_SpeechRecognized);
        }



        public void Destroy()
        {

            if (SpeechEngine != null)
            {
                SpeechEngine.Dispose();
            }
            if (kaSource != null)
            {
                kasSource.Close();
                kaSource.Stop();
                kaSource.Dispose();
            }
        }

        #endregion

        public void LoadSpeechActions(IList<SpeechAction> Root)
        {
            speechRoot = Root;
            speechCurrentNodes = Root;
            CreateGrammar();
            SpeechEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        protected void CreateGrammar()
        {
            GrammarBuilder gb = new Choices(
                speechCurrentNodes.Select(s => s.Name.ToLower()).ToArray()
                );
            SpeechEngine.UnloadAllGrammars();
            SpeechEngine.LoadGrammar(new Grammar(gb));
        }

        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            SpeechAction sa = speechCurrentNodes.FirstOrDefault(s => s.Name.ToLower() == e.Result.Text);
            if (sa == null)
                return;
            sa.TriggerAction();
            if ((sa.Children != null) && (sa.Children.Count != 0))
            {
                speechCurrentNodes = sa.Children;
                CreateGrammar();
            }
            else
            {
                speechCurrentNodes = speechRoot;
                CreateGrammar();
            }
        }
    }
}
