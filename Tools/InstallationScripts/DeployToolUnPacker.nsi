#region Copyright (C) 2005-2010 Team MediaPortal
/*
// Copyright (C) 2005-2010 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.
*/
#endregion

#**********************************************************************************************************#
#
#   For building the installer on your own you need:
#       1. Latest NSIS version from http://nsis.sourceforge.net/Download
#
#**********************************************************************************************************#
Name "MediaPortal Unpacker"
;SetCompressor /SOLID lzma

#---------------------------------------------------------------------------
# DEVELOPMENT ENVIRONMENT
#---------------------------------------------------------------------------
# path definitions
!define svn_ROOT "..\.."
!define svn_MP "${svn_ROOT}\mediaportal"
!define svn_TVServer "${svn_ROOT}\TvEngine3\TVLibrary"
!define svn_DeployTool "${svn_ROOT}\Tools\MediaPortal.DeployTool"
!define svn_InstallScripts "${svn_ROOT}\Tools\InstallationScripts"
!define svn_DeployVersionSVN "${svn_ROOT}\Tools\Script & Batch tools\DeployVersionSVN"


#---------------------------------------------------------------------------
# UNPACKER script
#---------------------------------------------------------------------------
!define PRODUCT_NAME          "MediaPortal"
!define PRODUCT_PUBLISHER     "Team MediaPortal"
!define PRODUCT_WEB_SITE      "www.team-mediaportal.com"

!define VER_MAJOR       1
!define VER_MINOR       2
!define VER_REVISION    0
!ifdef VER_BUILD ; means !build_release was used
  !undef VER_BUILD

  !system 'include-MP-PreBuild.bat'
  !include "version.txt"
  !delfile "version.txt"
  !if ${VER_BUILD} == 0
    !warning "It seems there was an error, reading the svn revision. 0 will be used."
  !endif
!else
  !define VER_BUILD       0
!endif

;this is for display purposes
!define VERSION "1.2.0"


#---------------------------------------------------------------------------
# BUILD sources
#---------------------------------------------------------------------------
; comment one of the following lines to disable the preBuild
!define BUILD_MediaPortal
!define BUILD_TVServer
!define BUILD_DeployTool
!define BUILD_Installer

!include "include-MP-PreBuild.nsh"


#---------------------------------------------------------------------------
# INCLUDE FILES
#---------------------------------------------------------------------------
!define NO_INSTALL_LOG
!include "${svn_InstallScripts}\include\LanguageMacros.nsh"
!include "${svn_InstallScripts}\include\MediaPortalMacros.nsh"


#---------------------------------------------------------------------------
# INSTALLER ATTRIBUTES
#---------------------------------------------------------------------------
Icon "${svn_DeployTool}\Install.ico"
!define /date buildTIMESTAMP "%Y-%m-%d-%H-%M"
!if ${VER_BUILD} == 0
  OutFile "MediaPortalSetup_${VERSION}_${buildTIMESTAMP}.exe"
!else
  OutFile "MediaPortalSetup_${VERSION}_SVN${VER_BUILD}_${buildTIMESTAMP}.exe"
!endif
InstallDir "$TEMP\MediaPortal Installation"

CRCCheck on
XPStyle on
RequestExecutionLevel admin
ShowInstDetails show
AutoCloseWindow true
VIProductVersion "${VER_MAJOR}.${VER_MINOR}.${VER_REVISION}.${VER_BUILD}"
VIAddVersionKey ProductName       "${PRODUCT_NAME}"
VIAddVersionKey ProductVersion    "${VERSION}"
VIAddVersionKey CompanyName       "${PRODUCT_PUBLISHER}"
VIAddVersionKey CompanyWebsite    "${PRODUCT_WEB_SITE}"
VIAddVersionKey FileVersion       "${VERSION}"
VIAddVersionKey FileDescription   "${PRODUCT_NAME} installation ${VERSION}"
VIAddVersionKey LegalCopyright    "Copyright � 2005-2009 ${PRODUCT_PUBLISHER}"

;if we want to make it fully silent we can uncomment this
;SilentInstall silent

;Page directory
Page instfiles

!insertmacro LANG_LOAD "English"

;sections for unpacking
Section
  IfFileExists "$INSTDIR\*.*" 0 +2
    RMDir /r "$INSTDIR"

  SetOutPath $INSTDIR
  File /r /x .svn /x *.pdb /x *.vshost.exe "${svn_DeployTool}\bin\Release\*"

  SetOutPath $INSTDIR\deploy
  File "${svn_MP}\Setup\Release\package-mediaportal.exe"
  File "${svn_TVServer}\Setup\Release\package-tvengine.exe"

  SetOutPath $INSTDIR\HelpContent\SetupGuide
  File /r /x .svn "${svn_DeployTool}\HelpContent\SetupGuide\*"
  
  SetOutPath $INSTDIR\HelpContent\DeployToolGuide
  File /r /x .svn "${svn_DeployTool}\HelpContent\DeployToolGuide\*"

SectionEnd

Function .onInit
  !insertmacro MediaPortalNetFrameworkCheck
FunctionEnd

Function .onInstSuccess
  Exec "$INSTDIR\MediaPortal.DeployTool.exe"
FunctionEnd