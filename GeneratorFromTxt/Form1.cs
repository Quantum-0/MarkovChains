using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Markov;

namespace GeneratorFromTxt
{
    public partial class Form1 : Form
    {
        IExtendedMarkovGenerator generator = new TrigramMarkovGenerator();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                button1.Enabled = false;
                string fname = openFileDialog.FileName;
                label1.Text = "Reading file: " + fname;
                using (var sr = new StreamReader(fname))
                {
                    while (!sr.EndOfStream)
                    {
                        progressBar1.Value = (int)(100 * sr.BaseStream.Position / sr.BaseStream.Length);
                        this.Update();
                        generator.LearnText(sr.ReadLine());
                    }
                }
                label1.Text = "File: " + fname + "\nTrigrams count: " + generator.GetNGramCount(3) + "\nStarts: " + generator.GetStartNGramCount() + "\nEnds: " + generator.GetEndNGramCount();
                textBox1.Text = generator.GenerateText();
                listBox1.Items.AddRange(generator.GetStartWords().ToArray());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex <= 0)
            {
                textBox1.Text = "";
                for(int i = 0; i < numericUpDown1.Value; i++)
                    textBox1.Text += generator.GenerateText() + Environment.NewLine;
            }
            else
            {
                textBox1.Text = "";
                for (int i = 0; i < numericUpDown1.Value; i++)
                    textBox1.Text += generator.GenerateText(listBox1.SelectedItem as string) + Environment.NewLine;
            }
        }
    }
}
