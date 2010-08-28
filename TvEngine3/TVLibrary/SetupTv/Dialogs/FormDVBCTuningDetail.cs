using System;
using System.Windows.Forms;
using DirectShowLib.BDA;

namespace SetupTv.Dialogs
{
  public partial class FormDVBCTuningDetail : SetupControls.FormTuningDetailCommon
  {
    public FormDVBCTuningDetail()
    {
      InitializeComponent();
    }

    private void FormDVBCTuningDetail_Load(object sender, System.EventArgs e)
    {
      comboBoxDvbCModulation.Items.Clear();
      foreach (ModulationType modValue in Enum.GetValues(typeof(ModulationType)))
      {
        comboBoxDvbCModulation.Items.Add(modValue);
      }

      if (TuningDetail != null)
      {
        //Editing
        textBoxChannel.Text = TuningDetail.ChannelNumber.ToString();
        textboxFreq.Text = TuningDetail.Frequency.ToString();
        textBoxONID.Text = TuningDetail.NetworkId.ToString();
        textBoxTSID.Text = TuningDetail.TransportId.ToString();
        textBoxSID.Text = TuningDetail.ServiceId.ToString();
        textBoxSymbolRate.Text = TuningDetail.Symbolrate.ToString();
        textBoxDVBCPmt.Text = TuningDetail.PmtPid.ToString();
        textBoxDVBCProvider.Text = TuningDetail.Provider;
        checkBoxDVBCfta.Checked = TuningDetail.FreeToAir;
        comboBoxDvbCModulation.SelectedItem = (ModulationType)TuningDetail.Modulation;
      }
      else
      {
        textBoxChannel.Text = "";
        textboxFreq.Text = "";
        textBoxONID.Text = "";
        textBoxTSID.Text = "";
        textBoxSID.Text = "";
        textBoxSymbolRate.Text = "";
        textBoxDVBCPmt.Text = "";
        textBoxDVBCProvider.Text = "";
        checkBoxDVBCfta.Checked = false;
        comboBoxDvbCModulation.SelectedIndex = -1;
      }
    }

    private void mpButtonOk_Click(object sender, EventArgs e)
    {
      if (ValidateInput())
      {
        if (TuningDetail == null)
        {
          TuningDetail = CreateInitialTuningDetail();
        }
        UpdateTuningDetail();
        DialogResult = DialogResult.OK;
        Close();
      }
    }

    private void UpdateTuningDetail()
    {
      TuningDetail.ChannelNumber = Int32.Parse(textBoxChannel.Text);
      TuningDetail.Frequency = Convert.ToInt32(textboxFreq.Text);
      TuningDetail.NetworkId = Convert.ToInt32(textBoxONID.Text);
      TuningDetail.TransportId = Convert.ToInt32(textBoxTSID.Text);
      TuningDetail.ServiceId = Convert.ToInt32(textBoxSID.Text);
      TuningDetail.Symbolrate = Convert.ToInt32(textBoxSymbolRate.Text);
      TuningDetail.PmtPid = Convert.ToInt32(textBoxDVBCPmt.Text);
      TuningDetail.Provider = textBoxDVBCProvider.Text;
      TuningDetail.FreeToAir = checkBoxDVBCfta.Checked;
      TuningDetail.Modulation = (int)comboBoxDvbCModulation.SelectedItem;
      TuningDetail.ChannelType = 2;
    }

    private bool ValidateInput()
    {
      int freq, onid, tsid, sid, symbolrate, pmt;
      if (!Int32.TryParse(textboxFreq.Text, out freq))
      {
        MessageBox.Show(this, "Please enter a valid frequency!", "Incorrect input");
        return false;
      }
      if (!Int32.TryParse(textBoxONID.Text, out onid))
      {
        MessageBox.Show(this, "Please enter a valid network id!", "Incorrect input");
        return false;
      }
      if (!Int32.TryParse(textBoxTSID.Text, out tsid))
      {
        MessageBox.Show(this, "Please enter a valid transport id!", "Incorrect input");
        return false;
      }
      if (!Int32.TryParse(textBoxSID.Text, out sid))
      {
        MessageBox.Show(this, "Please enter a valid service id!", "Incorrect input");
        return false;
      }
      if (!Int32.TryParse(textBoxSymbolRate.Text, out symbolrate))
      {
        MessageBox.Show(this, "Please enter a valid symbolrate!", "Incorrect input");
        return false;
      }
      if (!Int32.TryParse(textBoxDVBCPmt.Text, out pmt))
      {
        MessageBox.Show(this, "Please enter a valid pmt id!", "Incorrect input");
        return false;
      }
      if (onid <= 0 || tsid < 0 || sid < 0)
      {
        MessageBox.Show(this, "Please enter a valid network, transport and service id!", "Incorrect input");
        return false;
      }
      return true;
    }

  }
}
