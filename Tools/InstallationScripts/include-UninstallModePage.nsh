#region Copyright (C) 2005-2009 Team MediaPortal

/* 
 *	Copyright (C) 2005-2009 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

#**********************************************************************************************************#
#
# This original header file is taken from:           http://nsis.sourceforge.net/Add/Remove_Functionality
#     and modified for our needs.
#
#**********************************************************************************************************#


#####    UnInstallMode page
Var UnInstallMode
Var uninstModePage.optBtn0
Var uninstModePage.optBtn0.state
Var uninstModePage.optBtn1
Var uninstModePage.optBtn1.state
Var uninstModePage.optBtn2
Var uninstModePage.optBtn2.state

Function un.UninstallModePage
  !insertmacro MUI_HEADER_TEXT "$(TEXT_UNMODE_HEADER)" "$(TEXT_UNMODE_HEADER2)"

  nsDialogs::Create /NOUNLOAD 1018

  ;${NSD_CreateLabel} 0 0 100% 24u "$(TEXT_UNMODE_INFO)"
  ;Pop $R1


  ${NSD_CreateRadioButton} 20u 20u -30u 8u "$(TEXT_UNMODE_OPT0)"
  Pop $uninstModePage.optBtn0
  ${NSD_OnClick} $uninstModePage.optBtn0 un.UninstallModePageUpdateSelection

  ${NSD_CreateLabel} 30u 30u 100% 24u "$(TEXT_UNMODE_OPT0_DESC)"


  ${NSD_CreateRadioButton} 20u 60u -30u 8u "$(TEXT_UNMODE_OPT1)"
  Pop $uninstModePage.optBtn1
  ${NSD_OnClick} $uninstModePage.optBtn1 un.UninstallModePageUpdateSelection

  ${NSD_CreateLabel} 30u 70u 100% 24u "$(TEXT_UNMODE_OPT1_DESC)"


  ${NSD_CreateRadioButton} 20u 100u -30u 8u "$(TEXT_UNMODE_OPT2)"
  Pop $uninstModePage.optBtn2
  ${NSD_OnClick} $uninstModePage.optBtn2 un.UninstallModePageUpdateSelection

  ${NSD_CreateLabel} 30u 110u 100% 24u "$(TEXT_UNMODE_OPT2_DESC)"


  SendMessage $uninstModePage.optBtn0 ${BM_SETCHECK} ${BST_CHECKED} 0

  nsDialogs::Show
FunctionEnd

Function un.UninstallModePageUpdateSelection

  ${NSD_GetState} $uninstModePage.optBtn0 $uninstModePage.optBtn0.state
  ${NSD_GetState} $uninstModePage.optBtn1 $uninstModePage.optBtn1.state
  ${NSD_GetState} $uninstModePage.optBtn2 $uninstModePage.optBtn2.state

  ${If} $uninstModePage.optBtn1.state == ${BST_CHECKED}
    StrCpy $UnInstallMode 1
  ${ElseIf} $uninstModePage.optBtn2.state == ${BST_CHECKED}
    StrCpy $UnInstallMode 2
  ${Else}
    StrCpy $UnInstallMode 0
  ${EndIf}

FunctionEnd

Function un.UninstallModePageLeave

  ${If} $UnInstallMode == 1
    MessageBox MB_YESNO|MB_ICONEXCLAMATION|MB_DEFBUTTON2 "$(TEXT_UNMODE_OPT1_MSGBOX)" IDYES +2
    Abort    
  ${ElseIf} $UnInstallMode == 2
    MessageBox MB_YESNO|MB_ICONEXCLAMATION|MB_DEFBUTTON2 "$(TEXT_UNMODE_OPT2_MSGBOX)" IDYES +2
    Abort
  ${EndIf}

FunctionEnd



LangString TEXT_UNMODE_HEADER     ${LANG_ENGLISH} "Uninstallation Mode"
LangString TEXT_UNMODE_HEADER2    ${LANG_ENGLISH} "Please choose the mode, you want to do the uninstallation."

LangString TEXT_UNMODE_OPT0       ${LANG_ENGLISH} "Standard Uninstall (recommended)"
LangString TEXT_UNMODE_OPT1       ${LANG_ENGLISH} "Complete Uninstallation for ${NAME}"
LangString TEXT_UNMODE_OPT2       ${LANG_ENGLISH} "Full MediaPortal Products cleanup"

LangString TEXT_UNMODE_OPT0_DESC  ${LANG_ENGLISH} "Only the main application will be uninstalled, userfiles and databases will not be deleted (recommended)"
LangString TEXT_UNMODE_OPT1_DESC  ${LANG_ENGLISH} "This will uninstall ${NAME}, delete all userfiles and databases"
LangString TEXT_UNMODE_OPT2_DESC  ${LANG_ENGLISH} "This will also remove all files, folders, databases, settings and registry keys which might be leftovers from older MediaPortal versions."


LangString TEXT_UNMODE_OPT1_MSGBOX  ${LANG_ENGLISH} "Are you sure that you want to do a Complete Uninstallation? This can not be undone!"
LangString TEXT_UNMODE_OPT2_MSGBOX  ${LANG_ENGLISH} "Are you sure that you want to do a Full MediaPortal Products cleanup? This can not be undone!"