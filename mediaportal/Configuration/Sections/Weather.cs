using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MediaPortal.Configuration.Sections
{
	public class Weather : MediaPortal.Configuration.SectionSettings
	{
		const int MaximumCities = 20;

		private MediaPortal.UserInterface.Controls.MPGroupBox groupBox1;
		private MediaPortal.UserInterface.Controls.MPGroupBox mpGroupBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox temperatureComboBox;
		private System.Windows.Forms.ComboBox windSpeedComboBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button removeButton;
		private System.Windows.Forms.Button searchButton;
		private MediaPortal.UserInterface.Controls.MPListView citiesListView;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.TextBox intervalTextBox;
    private System.Windows.Forms.Button editButton;
    private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.ComponentModel.IContainer components = null;

		public Weather() : this("Weather")
		{
		}

		public Weather(string name) : base(name)
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			//
			// Populate combo boxes with default values
			//
			temperatureComboBox.Items.AddRange(new string[] { "Celsius", "Fahrenheit" });
			windSpeedComboBox.Items.AddRange(new string[] { "m/s", "km/hour", "mph" });
		}

		/// <summary>
		/// 
		/// </summary>
		public override void LoadSettings()
		{
			using (AMS.Profile.Xml xmlreader = new AMS.Profile.Xml("MediaPortal.xml"))
			{
				string windSpeed = xmlreader.GetValueAsString("weather","speed","K");

				if(windSpeed.Equals("K"))
					windSpeedComboBox.Text = "km/hour";
				else if(windSpeed.Equals("M"))
					windSpeedComboBox.Text = "mph";
				else if(windSpeed.Equals("S"))
					windSpeedComboBox.Text = "m/s";

				string temperature = xmlreader.GetValueAsString("weather", "temperature", "C");

				if(temperature.Equals("C"))
					temperatureComboBox.Text = "Celsius";
				else if(temperature.Equals("F"))
					temperatureComboBox.Text = "Fahrenheit";

				intervalTextBox.Text = Convert.ToString(xmlreader.GetValueAsInt("weather", "refresh", 30));

				for(int index = 0; index < MaximumCities; index++)
				{
					string cityName = String.Format("city{0}", index);
					string cityCode = String.Format("code{0}", index);
          string citySat = String.Format("sat{0}", index);

					string cityNameData = xmlreader.GetValueAsString("weather", cityName, "");
					string cityCodeData = xmlreader.GetValueAsString("weather", cityCode, "");
          string citySatData  = xmlreader.GetValueAsString("weather", citySat, "");

					if(cityNameData.Length > 0 && cityCodeData.Length > 0)
						citiesListView.Items.Add(new ListViewItem(new string[] { cityNameData, cityCodeData, citySatData }));
				}
			}			
		}

		public override void SaveSettings()
		{
			using (AMS.Profile.Xml xmlwriter = new AMS.Profile.Xml("MediaPortal.xml"))
			{
				string windSpeed = String.Empty;

				if(windSpeedComboBox.Text.Equals("km/hour"))
					windSpeed = "K";
				else if(windSpeedComboBox.Text.Equals("mph"))
					 windSpeed = "M";
				else if(windSpeedComboBox.Text.Equals("m/s"))
					 windSpeed = "S";

				xmlwriter.SetValue("weather", "speed", windSpeed);

				string temperature = String.Empty;

				if(temperatureComboBox.Text.Equals("Celsius"))
					 temperature = "C";
				else if(temperatureComboBox.Text.Equals("Fahrenheit"))
					temperature = "F";

				xmlwriter.SetValue("weather", "temperature", temperature);

				xmlwriter.SetValue("weather", "refresh", intervalTextBox.Text);

				for(int index = 0; index < MaximumCities; index++)
				{
					string cityName = String.Format("city{0}", index);
					string cityCode = String.Format("code{0}", index);
          string citySat  = String.Format("sat{0}", index);

					string cityNameData = String.Empty;
					string cityCodeData = String.Empty;
          string citySatData = String.Empty;

					if(citiesListView.Items != null && citiesListView.Items.Count > index)
					{
						cityNameData = citiesListView.Items[index].SubItems[0].Text;
						cityCodeData = citiesListView.Items[index].SubItems[1].Text;
            citySatData  = citiesListView.Items[index].SubItems[2].Text;
					}

					xmlwriter.SetValue("weather", cityName, cityNameData);
					xmlwriter.SetValue("weather", cityCode, cityCodeData);
          xmlwriter.SetValue("weather", citySat, citySatData);
				}
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
      this.groupBox1 = new MediaPortal.UserInterface.Controls.MPGroupBox();
      this.intervalTextBox = new System.Windows.Forms.TextBox();
      this.label3 = new System.Windows.Forms.Label();
      this.windSpeedComboBox = new System.Windows.Forms.ComboBox();
      this.label2 = new System.Windows.Forms.Label();
      this.temperatureComboBox = new System.Windows.Forms.ComboBox();
      this.label1 = new System.Windows.Forms.Label();
      this.mpGroupBox1 = new MediaPortal.UserInterface.Controls.MPGroupBox();
      this.editButton = new System.Windows.Forms.Button();
      this.searchButton = new System.Windows.Forms.Button();
      this.removeButton = new System.Windows.Forms.Button();
      this.citiesListView = new MediaPortal.UserInterface.Controls.MPListView();
      this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
      this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
      this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
      this.groupBox1.SuspendLayout();
      this.mpGroupBox1.SuspendLayout();
      this.SuspendLayout();
      // 
      // groupBox1
      // 
      this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
      this.groupBox1.Controls.Add(this.intervalTextBox);
      this.groupBox1.Controls.Add(this.label3);
      this.groupBox1.Controls.Add(this.windSpeedComboBox);
      this.groupBox1.Controls.Add(this.label2);
      this.groupBox1.Controls.Add(this.temperatureComboBox);
      this.groupBox1.Controls.Add(this.label1);
      this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.groupBox1.Location = new System.Drawing.Point(8, 8);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(440, 128);
      this.groupBox1.TabIndex = 0;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "General settings";
      // 
      // intervalTextBox
      // 
      this.intervalTextBox.Location = new System.Drawing.Point(168, 77);
      this.intervalTextBox.Name = "intervalTextBox";
      this.intervalTextBox.Size = new System.Drawing.Size(40, 20);
      this.intervalTextBox.TabIndex = 5;
      this.intervalTextBox.Text = "";
      // 
      // label3
      // 
      this.label3.Location = new System.Drawing.Point(16, 80);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(150, 23);
      this.label3.TabIndex = 4;
      this.label3.Text = "Refresh interval (minutes)";
      // 
      // windSpeedComboBox
      // 
      this.windSpeedComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
      this.windSpeedComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.windSpeedComboBox.Location = new System.Drawing.Point(168, 52);
      this.windSpeedComboBox.Name = "windSpeedComboBox";
      this.windSpeedComboBox.Size = new System.Drawing.Size(256, 21);
      this.windSpeedComboBox.TabIndex = 3;
      // 
      // label2
      // 
      this.label2.Location = new System.Drawing.Point(16, 55);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(150, 23);
      this.label2.TabIndex = 2;
      this.label2.Text = "Wind speed shown as";
      // 
      // temperatureComboBox
      // 
      this.temperatureComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
      this.temperatureComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.temperatureComboBox.Location = new System.Drawing.Point(168, 27);
      this.temperatureComboBox.Name = "temperatureComboBox";
      this.temperatureComboBox.Size = new System.Drawing.Size(256, 21);
      this.temperatureComboBox.TabIndex = 1;
      // 
      // label1
      // 
      this.label1.Location = new System.Drawing.Point(16, 30);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(150, 23);
      this.label1.TabIndex = 0;
      this.label1.Text = "Temperature shown in";
      // 
      // mpGroupBox1
      // 
      this.mpGroupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
      this.mpGroupBox1.Controls.Add(this.editButton);
      this.mpGroupBox1.Controls.Add(this.searchButton);
      this.mpGroupBox1.Controls.Add(this.removeButton);
      this.mpGroupBox1.Controls.Add(this.citiesListView);
      this.mpGroupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.mpGroupBox1.Location = new System.Drawing.Point(8, 144);
      this.mpGroupBox1.Name = "mpGroupBox1";
      this.mpGroupBox1.Size = new System.Drawing.Size(440, 160);
      this.mpGroupBox1.TabIndex = 1;
      this.mpGroupBox1.TabStop = false;
      this.mpGroupBox1.Text = "Cities";
      // 
      // editButton
      // 
      this.editButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.editButton.Enabled = false;
      this.editButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.editButton.Location = new System.Drawing.Point(176, 126);
      this.editButton.Name = "editButton";
      this.editButton.TabIndex = 3;
      this.editButton.Text = "Edit";
      this.editButton.Click += new System.EventHandler(this.editButton_Click);
      // 
      // searchButton
      // 
      this.searchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.searchButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.searchButton.Location = new System.Drawing.Point(16, 126);
      this.searchButton.Name = "searchButton";
      this.searchButton.TabIndex = 2;
      this.searchButton.Text = "Search";
      this.searchButton.Click += new System.EventHandler(this.searchButton_Click);
      // 
      // removeButton
      // 
      this.removeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.removeButton.Enabled = false;
      this.removeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.removeButton.Location = new System.Drawing.Point(96, 126);
      this.removeButton.Name = "removeButton";
      this.removeButton.TabIndex = 1;
      this.removeButton.Text = "Remove";
      this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
      // 
      // citiesListView
      // 
      this.citiesListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
        | System.Windows.Forms.AnchorStyles.Left) 
        | System.Windows.Forms.AnchorStyles.Right)));
      this.citiesListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                     this.columnHeader1,
                                                                                     this.columnHeader2,
                                                                                     this.columnHeader3});
      this.citiesListView.FullRowSelect = true;
      this.citiesListView.Location = new System.Drawing.Point(16, 24);
      this.citiesListView.Name = "citiesListView";
      this.citiesListView.Size = new System.Drawing.Size(408, 96);
      this.citiesListView.TabIndex = 0;
      this.citiesListView.View = System.Windows.Forms.View.Details;
      this.citiesListView.SelectedIndexChanged += new System.EventHandler(this.citiesListView_SelectedIndexChanged);
      // 
      // columnHeader1
      // 
      this.columnHeader1.Text = "City";
      this.columnHeader1.Width = 125;
      // 
      // columnHeader2
      // 
      this.columnHeader2.Text = "Code";
      this.columnHeader2.Width = 78;
      // 
      // columnHeader3
      // 
      this.columnHeader3.Text = "Satellite image";
      this.columnHeader3.Width = 201;
      // 
      // Weather
      // 
      this.Controls.Add(this.mpGroupBox1);
      this.Controls.Add(this.groupBox1);
      this.Name = "Weather";
      this.Size = new System.Drawing.Size(456, 448);
      this.groupBox1.ResumeLayout(false);
      this.mpGroupBox1.ResumeLayout(false);
      this.ResumeLayout(false);

    }
		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void searchButton_Click(object sender, System.EventArgs e)
		{
			SearchCityForm searchForm = new SearchCityForm();
			
			//
			// Show dialog
			//
			if(searchForm.ShowDialog(this) == DialogResult.OK)
			{
				//
				// Fetch selected cities
				//
				ArrayList cities = searchForm.SelectedCities;

				foreach(WeatherChannel.City city in cities)
				{
					citiesListView.Items.Add(new ListViewItem(new string[] { city.Name, city.Id, string.Empty }));
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void removeButton_Click(object sender, System.EventArgs e)
		{
			int numberItems = citiesListView.SelectedItems.Count;

			for(int index = 0; index < numberItems; index++)
			{
				citiesListView.Items.RemoveAt(citiesListView.SelectedIndices[0]);
			}
		}

    private void editButton_Click(object sender, System.EventArgs e)
    {
      foreach(ListViewItem listItem in citiesListView.SelectedItems)
      {
        EditWeatherCityForm cityForm = new EditWeatherCityForm();

        //
        // Set current image location
        //
        cityForm.SatteliteImage = listItem.SubItems[2].Text;

        DialogResult dialogResult = cityForm.ShowDialog(this);

        if(dialogResult == DialogResult.OK)
        {
          //
          // Fetch selected image location
          //
          listItem.SubItems[2].Text = cityForm.SatteliteImage;
        }
      }
    }

    private void citiesListView_SelectedIndexChanged(object sender, System.EventArgs e)
    {
      editButton.Enabled = removeButton.Enabled = (citiesListView.SelectedItems.Count > 0);
    }
	}
}

