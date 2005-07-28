using System;
using System.ComponentModel;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Windows.Serialization;

using MediaPortal.Layouts;

namespace MediaPortal.GUI.Library
{
	/// <summary>
	/// Creates new GUIControls based on Skin Xml data, and handles
	/// the skin references.xml file which contains default properties for most controls.
	/// </summary>
	public class GUIControlFactory
	{
		#region Constructors

		private GUIControlFactory() // NON-CREATABLE
		{
		}
		
		#endregion Constructors

		#region Methods

		public static void LoadReferences(string referenceFile)
		{
			try
			{		
				if (m_referenceNodesByControlType != null)
					return;
				Log.Write("  Loading references from {0}", referenceFile);
				m_referenceNodesByControlType = new Hashtable();
				XmlDocument doc = new XmlDocument();
				doc.Load(referenceFile);
				// Check the root element
				if (doc.DocumentElement == null || doc.DocumentElement.Name != "controls")
					return;

				ReadSkinSizeFromReferenceFile(doc);

				XmlNodeList list = doc.DocumentElement.SelectNodes("/controls/control");
				foreach (XmlNode controlNode in list)
				{
					if (GetControlType(controlNode) != null)
						m_referenceNodesByControlType[GetControlType(controlNode)] = controlNode;
				}
			}
			catch (Exception ex)
			{
				Log.Write("exception loading references {0} err:{1} stack:{2}",
					referenceFile, ex.Message, ex.StackTrace);
			}
		}

		
		/// <summary>
		/// Deletes all reference nodes from memory (Use this to change skins in runtime).
		/// </summary>
		public static void ClearReferences()
		{
			m_referenceNodesByControlType = null;
		}

		private static void ReadSkinSizeFromReferenceFile(XmlDocument doc)
		{
			GUIGraphicsContext.SkinSize = new Size(720, 576);
			XmlNode nodeSkinWidth = doc.DocumentElement.SelectSingleNode("/controls/skin/width/text()");
			XmlNode nodeSkinHeight = doc.DocumentElement.SelectSingleNode("/controls/skin/height/text()");
			if (nodeSkinWidth != null && nodeSkinHeight != null)
			{
				try
				{
					int iWidth = Convert.ToInt16(nodeSkinWidth.Value);
					int iHeight = Convert.ToInt16(nodeSkinHeight.Value);
					Log.Write("  original skin size:{0}x{1}", iWidth, iHeight);
					GUIGraphicsContext.SkinSize = new Size(iWidth, iHeight);
				}
				catch (FormatException) // Size values were invalid.
				{
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="guiControlType">The type of control you wish to update.</param>
		/// <returns>A hashtable which contains the FieldInfo objects for every
		/// updatable field, indexed by their corresponding Xml Element name. </returns>
		static Hashtable GetFieldsToUpdate(Type guiControlType)
		{
			// Lazy Initializiation...
			if (m_reflectionCacheByControlType.ContainsKey(guiControlType)) 
				return (Hashtable)m_reflectionCacheByControlType[guiControlType]; 
			
			Hashtable fieldsTable = new Hashtable();
			FieldInfo[] allFields = guiControlType.GetFields(
				BindingFlags.Instance 
				|BindingFlags.NonPublic
				|BindingFlags.FlattenHierarchy
				|BindingFlags.Public);
			foreach (FieldInfo field in allFields)
			{
				if (field.IsDefined(typeof(XMLSkinElementAttribute), false))
				{
					XMLSkinElementAttribute atrb = (XMLSkinElementAttribute)
						field.GetCustomAttributes(typeof(XMLSkinElementAttribute), false)[0];
					fieldsTable[atrb.XmlElementName] = field;
				}
			}
			m_reflectionCacheByControlType[guiControlType] = fieldsTable;
			return fieldsTable;
						
		}

		private static object ConvertXmlStringToObject(string valueName, string valueText, Type type)
		{
			try
			{
				try
				{
					if(type == typeof (string))
						return valueText;
				}
				catch (Exception) 
				{ 
					return String.Empty;
				}

				try
				{
					if(type == typeof(int) || type == typeof(long))
					{
						switch(valueName.ToLower())
						{
							case "textcolor":
							case "colorkey":
							case "colordiffuse":
								if(valueText.Length > 0)
								{
									bool isNamedColor = false;

									foreach(char ch in valueText)
									{
										if(ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f')
											continue;

										isNamedColor = true;
										break;
									}

									if(isNamedColor)
									{
										int index = valueText.IndexOf(':');

										if(index != -1)
										{
											Color color = ColorTranslator.FromHtml(valueText.Substring(0, index));
											int alpha = 255;

											if(index < valueText.Length)
											{
												if(valueText[index + 1] == '#')
													alpha = int.Parse(valueText.Substring(index + 2), NumberStyles.HexNumber);
												else
													alpha = int.Parse(valueText.Substring(index + 1));
											}

											return Color.FromArgb(alpha, color).ToArgb();
										}

										return Color.FromName(valueText).ToArgb();
									}

									return ColorTranslator.FromHtml('#' + valueText).ToArgb();
								}

								break;
						}
					}

					if (type == typeof (int))
						return System.Int32.Parse(valueText);
					if (type == typeof (long))
						return System.Int64.Parse(valueText, NumberStyles.HexNumber);
				}
				catch(Exception)
				{
					return 0;
				}
				try
				{
					if (type == typeof (bool))
						if (valueText == "off" || valueText == "no" || valueText == "disabled") 
							return false;
						else 
							return true;
				}
				catch(Exception)
				{
					return false;
				}
				try
				{
					if (type == typeof (GUIControl.Alignment))
					{
						switch (valueText)
						{
							case "right" :
								return GUIControl.Alignment.ALIGN_RIGHT;
							case "center" :
								return GUIControl.Alignment.ALIGN_CENTER;
							default:
								return GUIControl.Alignment.ALIGN_LEFT;
						}
					}
				}
				catch(Exception)
				{
					return GUIControl.Alignment.ALIGN_LEFT;
				}

				try
				{
					if (type == typeof (GUIControl.eOrientation))
					{
						switch (valueText)
						{
							case "horizontal" :
								return GUIControl.eOrientation.Horizontal;
							case "vertical" :
								return GUIControl.eOrientation.Vertical;
							default:
								return GUIControl.eOrientation.Horizontal;
						}
					}
				}
				catch(Exception)
				{
					return GUIControl.Alignment.ALIGN_LEFT;
				}


				try
				{
					if (type == typeof (Animator.AnimationType))
					{
						switch (valueText.ToLower())
						{
							case "flyinfromleft":
								return Animator.AnimationType.FlyInFromLeft;
							case "flyinfromright":
								return Animator.AnimationType.FlyInFromRight;
							case "flyinfromtop":
								return Animator.AnimationType.FlyInFromTop;
							case "flyinfrombottom":
								return Animator.AnimationType.FlyInFromBottom;
							case "zoominfrommiddle":
								return Animator.AnimationType.ZoomInFromMiddle;
						}
					}
				}
				catch(Exception)
				{
					return Animator.AnimationType.FlyInFromLeft;
				}
				try
				{
					if (type == typeof(GUISpinControl.SpinType))
					{
						switch (valueText.ToLower())
						{
							case "int": 
								return GUISpinControl.SpinType.SPIN_CONTROL_TYPE_INT;
							case "float":
								return GUISpinControl.SpinType.SPIN_CONTROL_TYPE_FLOAT;
							default: 
								return GUISpinControl.SpinType.SPIN_CONTROL_TYPE_TEXT;
						}
					}
				}
				catch(Exception)
				{
					return GUISpinControl.SpinType.SPIN_CONTROL_TYPE_INT;
				}

				if(type == typeof(ILayout))
					return ParseLayout(valueText);
			
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static GUIControl Create(int dwParentId, XmlNode pControlNode, IDictionary defines)
		{
			Type typeOfControlToCreate = GetControlType(pControlNode);
			if (typeOfControlToCreate == null)
				return null;

			object[] ctorParams = { dwParentId };			
			GUIControl control = (GUIControl)
				Activator.CreateInstance(typeOfControlToCreate,ctorParams);

			try
			{
				if(control is ISupportInitialize)
					((ISupportInitialize)control).BeginInit();
				
				XmlNode referenceNode = 
					(XmlNode) m_referenceNodesByControlType[typeOfControlToCreate];
				
				if (referenceNode != null)
					UpdateControlWithXmlData(control,typeOfControlToCreate, referenceNode, defines);
				
				UpdateControlWithXmlData(control,typeOfControlToCreate, pControlNode, defines);
				
				control.ScaleToScreenResolution();
				AddSubitemsToControl(pControlNode,control);
				control.FinalizeConstruction();
				
				if(control is IAddChild)
				{
					foreach(XmlNode subControlNode in pControlNode.SelectNodes("control"))
						((IAddChild)control).AddChild(Create(dwParentId, subControlNode, defines));
				}

				if (typeOfControlToCreate == typeof (GUIFacadeControl))
				{
					GUIFacadeControl facade = (GUIFacadeControl) control;
					XmlNodeList nodeList = pControlNode.SelectNodes("control");
					foreach (XmlNode subControlNode in nodeList)
					{
						GUIControl subControl = Create(dwParentId, subControlNode, defines);
						if (subControl is GUIListControl)
						{
							GUIListControl list = subControl as GUIListControl;
							if ( list.SubType=="album")
								facade.AlbumListView = list;
							else
								facade.ListView = list;
						}
						if (subControl is GUIThumbnailPanel)
							facade.ThumbnailView = subControl as GUIThumbnailPanel;
						if (subControl is GUIFilmstripControl)
							facade.FilmstripView = subControl as GUIFilmstripControl;
					}
				}

				if(control is ISupportInitialize)
					((ISupportInitialize)control).EndInit();
			}
			catch(Exception e)
			{
				Log.Write("Buffer: {0}", e.Message);
			}

			return control;
		}

		private static void UpdateControlWithXmlData(GUIControl control, 
			Type controlType,
			XmlNode pControlNode, IDictionary defines)
		{
			Hashtable fieldsThatCanBeUpdated = GetFieldsToUpdate(controlType);

			XmlNodeList childNodes = pControlNode.ChildNodes;
			foreach (XmlNode element in childNodes)
			{
				FieldInfo correspondingField =
					fieldsThatCanBeUpdated[element.Name] as FieldInfo;

				if (correspondingField != null)
				{
					string text = element.InnerText;

					if(defines.Contains(text))
						text = (string)defines[text];

					object newValue = ConvertXmlStringToObject(element.Name, text,
						correspondingField.FieldType);
			
					try
					{
						correspondingField.SetValue(control, newValue);
					}
					catch (Exception e)
					{
						Log.Write("Couldn't place {0}, which is {1} in {2}. Exception:{3}",  
							newValue, newValue.GetType(), correspondingField,e) ;
					}

				}
			}

		}

		private static void AddSubitemsToControl(XmlNode subItemsNode, GUIControl control)
		{
			XmlNodeList subNodes=subItemsNode.SelectNodes("subitems/subitem/text()");
			foreach (XmlNode subNode in subNodes)
			{
				string strSubItem = subNode.Value;
				if (Char.IsDigit(strSubItem[0]))
				{
					GUILocalizeStrings.LocalizeLabel(ref strSubItem);
				}
				control.AddSubItem(strSubItem);
			}
		}

		private static Type GetControlType(XmlNode controlNode)
		{
			XmlNode typeText = controlNode.SelectSingleNode("type/text()");
			if (typeText == null || typeText.Value == "")
				return null;
			string xmlTypeName = typeText.Value;
			switch (xmlTypeName)
			{
				case ("image"):
					return typeof (GUIImage);
				case ("imagelist"):
					return typeof (GUIImageList);
				case ("slider"):
					return typeof (GUISliderControl);
				case ("fadelabel"):
					return typeof (GUIFadeLabel);
				case ("label"):
					return typeof (GUILabelControl);
				case ("button"):
					return typeof (GUIButtonControl);
				case ("updownbutton"):
					return typeof (GUIUpDownButton);
				case ("button3part"):
					return typeof (GUIButton3PartControl);
				case ("statusbar"):
					return typeof (GUIStatusbarControl);
				case ("progress"):
					return typeof (GUIProgressControl);
				case ("tvprogress"):
					return typeof (GUITVProgressControl);
				case ("hscrollbar"):
					return typeof (GUIHorizontalScrollbar);
				case ("vscrollbar"):
					return typeof (GUIverticalScrollbar);
				case ("textbox"):
					return typeof (GUITextControl);
				case ("textboxscrollup"):
					return typeof (GUITextScrollUpControl);
				case ("thumbnailpanel"):
					return typeof (GUIThumbnailPanel);
				case ("spincontrol"):
					return typeof (GUISpinControl);
				case ("checkmark"):
					return typeof (GUICheckMarkControl);
				case ("selectbutton"):
					return typeof (GUISelectButtonControl);
				case ("listcontrol"):
					return typeof (GUIListControl);
				case ("updownlistcontrol"):
					return typeof (GUIUpDownListControl);
				case ("checklistcontrol"):
					return typeof (GUICheckListControl);
				case ("togglebutton"):
					return typeof (GUIToggleButtonControl);
				case ("group"):
					return typeof (GUIGroup);
				case ("videowindow"):
					return typeof (GUIVideoControl);
				case ("facadeview"):
					return typeof (GUIFacadeControl);
				case ("filmstrip"):
					return typeof (GUIFilmstripControl);
				case ("smsinput"):
					return typeof (GUISMSInputControl);
				case ("sortbutton"):
					return typeof (GUISortButtonControl);
				default:
					Type t = (Type)m_hashCustomControls[xmlTypeName];

					if(t == null)
					{
						Log.Write("ERROR: unknown control:<{0}>",xmlTypeName);
						return null;
					}

					return t;
			}
		}

		public static void RegisterControl(string strName, Type t)
		{
			m_hashCustomControls[strName] = t;
		}

		static object ParseLayout(string valueText)
		{
			int openingBracket = valueText.IndexOf('(');
			int[] valueParameters = null;

			string layoutClass = string.Empty;

			if(openingBracket != -1)
			{
				layoutClass = valueText.Substring(0, openingBracket);
				valueParameters = ParseParameters(valueText.Substring(openingBracket).Trim());
			}
			else
			{
				layoutClass = valueText;
				valueParameters = new int[0];
			}

			if(layoutClass.ToLower() == "gridlayout")
			{
				if(valueParameters.Length >= 5)
					return new GridLayout(valueParameters[0], valueParameters[1], valueParameters[2], valueParameters[3], (Orientation)valueParameters[4]);

				if(valueParameters.Length >= 4)
					return new GridLayout(valueParameters[0], valueParameters[1], valueParameters[2], valueParameters[3]);

				if(valueParameters.Length >= 2)
					return new GridLayout(valueParameters[0], valueParameters[1]);

				if(valueParameters.Length >= 1)
					return new GridLayout(valueParameters[0]);

				if(valueParameters.Length == 0)
					return new GridLayout();

				return null;
			}

			if(layoutClass.ToLower() == "stacklayout")
			{
				if(valueParameters.Length >= 2)
					return new StackLayout(valueParameters[0], (Orientation)valueParameters[1]);

				if(valueParameters.Length >= 1)
					return new StackLayout(valueParameters[0]);

				if(valueParameters.Length == 0)
					return new StackLayout();

				return null;
			}

			return null;
		}

		static int[] ParseParameters(string valueText)
		{
			if(!(valueText.StartsWith("(") && valueText.EndsWith(")")))
				return new int[0];

			valueText = valueText.Substring(1, valueText.Length - 2);

			try
			{
				ArrayList valuesTemp = new ArrayList();

				foreach(string token in valueText.Split(new char[] { ',', ' ' }))
				{
					if(token == string.Empty)
						continue;

					if(string.Compare(token, "Horizontal") == 0)
					{
						valuesTemp.Add((int)Orientation.Horizontal);
					}
					else if(string.Compare(token, "Vertical") == 0)
					{
						valuesTemp.Add((int)Orientation.Vertical);
					}
					else
					{
						valuesTemp.Add(int.Parse(token));	
					}
				}

				int[] values = new int[valuesTemp.Count];

				Array.Copy(valuesTemp.ToArray(), values, values.Length);

				return values;
			}
			catch { }

			return new int[0];
		}

		#endregion Methods

		#region Fields

		/// <summary>
		/// Contains all of the reference nodes, indexed by control Type.
		/// </summary>
		static Hashtable			m_referenceNodesByControlType = null;

		static Hashtable			m_hashCustomControls = new Hashtable();

		/// <summary>
		/// A hashtable which contains the reflection results for every control.
		/// </summary>
		static Hashtable m_reflectionCacheByControlType = new Hashtable(20);

		#endregion Fields
	}
}