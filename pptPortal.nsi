!define APPNAME "pptPortal"
!define EXENAME "pptPortal.exe"
!define INSTALLDIR "$PROGRAMFILES\${APPNAME}"
!define SOURCE_PUBLISH_DIR "C:\\temp file transfer\\9.VisualStudio\\field testing\\pptPortal\\pptPortal\\bin\\publish"
!define ICON_PATH "C:\\temp file transfer\\9.VisualStudio\\field testing\\pptPortal\\pptPortal\\bin\\publish\\pptPortal.ico"


; Build output locations
!define NSIS_OUTPUT_DIR "C:\\temp file transfer\\9.VisualStudio\\exe wrapper scripts\\NSIS Output"
!define SOLUTION_OUTPUT_DIR "C:\\temp file transfer\\9.VisualStudio\\field testing\\pptPortal"

!include "FileFunc.nsh"
!include "x64.nsh"
!include "MUI2.nsh"
!include "nsDialogs.nsh"

; Build primary output in NSIS Output
OutFile "${NSIS_OUTPUT_DIR}\\${APPNAME}_Install.exe"
Name "${APPNAME}"
InstallDir "${INSTALLDIR}"

; Copy this .nsi to the solution folder at compile time
!system 'cmd /C if not exist "${SOLUTION_OUTPUT_DIR}" mkdir "${SOLUTION_OUTPUT_DIR}"'
!system 'cmd /C copy /Y "${__FILE__}" "${SOLUTION_OUTPUT_DIR}\\${APPNAME}.nsi" >nul'

; Copy compiled installer exe to the solution folder when compile completes
!finalize 'cmd /C copy /Y "%1" "${SOLUTION_OUTPUT_DIR}\\${APPNAME}_Install.exe" >nul'

RequestExecutionLevel admin

!define MUI_ABORTWARNING
!define MUI_ICON "${ICON_PATH}"

Var MAINTENANCE_MODE
Var MAINTENANCE_CHOICE
Var DESKTOP_CHECKED

Function CloseAppIfRunning
    nsExec::ExecToLog 'taskkill /F /IM "${EXENAME}"'
    Sleep 1000
FunctionEnd

Function WelcomePagePre
    StrCmp $MAINTENANCE_MODE "" show_welcome
    Abort
    show_welcome:
FunctionEnd

Function MaintenancePageLeave
    ${NSD_GetState} $1 $MAINTENANCE_CHOICE
    ${If} $MAINTENANCE_CHOICE == ${BST_CHECKED}
        StrCpy $MAINTENANCE_CHOICE "update"
    ${Else}
        ${NSD_GetState} $2 $MAINTENANCE_CHOICE
        ${If} $MAINTENANCE_CHOICE == ${BST_CHECKED}
            StrCpy $MAINTENANCE_CHOICE "uninstall"
            MessageBox MB_YESNO|MB_ICONQUESTION "Are you sure you want to uninstall ${APPNAME}?" IDYES mpl_confirmed
            Abort
            mpl_confirmed:
            Call CloseAppIfRunning
            ExecWait '"$INSTDIR\\Uninstall.exe"'
            Quit
        ${Else}
            StrCpy $MAINTENANCE_CHOICE "cancel"
            Quit
        ${EndIf}
    ${EndIf}
FunctionEnd


Function .onInit
    ReadRegStr $MAINTENANCE_MODE HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName"
    StrCpy $DESKTOP_CHECKED ${BST_CHECKED}
FunctionEnd

Function MaintenancePageShow
    StrCmp $MAINTENANCE_MODE "${APPNAME}" 0 skip
    !insertmacro MUI_HEADER_TEXT "${APPNAME} Maintenance" "Choose an action:"
    nsDialogs::Create 1018
    Pop $0
    ${If} $0 == error
        Abort
    ${EndIf}
    ${NSD_CreateRadioButton} 0u 20u 100% 12u "Update/Repair ${APPNAME}"
    Pop $1
    ${NSD_SetState} $1 ${BST_CHECKED}
    ${NSD_CreateRadioButton} 0u 40u 100% 12u "Uninstall ${APPNAME}"
    Pop $2
    ${NSD_CreateRadioButton} 0u 60u 100% 12u "Cancel"
    Pop $3
    nsDialogs::Show
    Return
    skip:
        Abort
FunctionEnd

Function OptionsPageShow
    StrCmp $MAINTENANCE_MODE "${APPNAME}" 0 show_options
    StrCmp $MAINTENANCE_CHOICE "update" skip_options
    Goto show_options
    skip_options:
        Abort
    show_options:
    !insertmacro MUI_HEADER_TEXT "Installation Options" "Choose additional tasks:"
    nsDialogs::Create 1018
    Pop $0
    ${If} $0 == error
        Abort
    ${EndIf}
    ${NSD_CreateCheckbox} 0u 40u 100% 12u "Add shortcut to Public Desktop"
    Pop $1
    ${NSD_SetState} $1 $DESKTOP_CHECKED
    nsDialogs::Show
FunctionEnd

Function OptionsPageLeave
    ${NSD_GetState} $1 $DESKTOP_CHECKED
FunctionEnd

!define MUI_PAGE_CUSTOMFUNCTION_PRE WelcomePagePre
!insertmacro MUI_PAGE_WELCOME

Page custom MaintenancePageShow MaintenancePageLeave
Page custom OptionsPageShow OptionsPageLeave

!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\${EXENAME}"
!define MUI_FINISHPAGE_RUN_TEXT "Run ${APPNAME}"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

;--------------------------------
; Version Information
;--------------------------------
VIProductVersion "1.0.0.0"
VIAddVersionKey "CompanyName" "Your Company"
VIAddVersionKey "LegalCopyright" "Copyright (c) 2026"
VIAddVersionKey "FileVersion" "1.0.0.0"
VIAddVersionKey "ProductVersion" "1.0.0.0"
VIAddVersionKey "Author" "Your Name"
VIAddVersionKey "FileDescription" "${APPNAME} installer"
VIAddVersionKey "InternalName" "${APPNAME}"

;--------------------------------
; Installer Icon
;--------------------------------
Icon "${ICON_PATH}"

Section "Install"
    StrCmp $MAINTENANCE_MODE "${APPNAME}" is_maintenance
    Goto do_install

    is_maintenance:
        Call CloseAppIfRunning
        Goto update_only

    update_only:
        SetOutPath "$INSTDIR"
        File /r /x "*.pdb" "${SOURCE_PUBLISH_DIR}\\*.*"
        WriteUninstaller "$INSTDIR\Uninstall.exe"
        SetShellVarContext all
        CreateDirectory "$SMPROGRAMS\${APPNAME}"
        CreateShortcut "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${EXENAME}"
        ${If} $DESKTOP_CHECKED == 1
            CreateShortcut "$Desktop\${APPNAME}.lnk" "$INSTDIR\${EXENAME}"
        ${EndIf}
        WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
        WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
        WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayIcon" "$INSTDIR\${EXENAME}"
        WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayVersion" "1.0.0.0"
        WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoModify" 1
        WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoRepair" 1
        Goto end_maint

    do_install:
    SetOutPath "$INSTDIR"
    File /r /x "*.pdb" "${SOURCE_PUBLISH_DIR}\\*.*"
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    SetShellVarContext all
    CreateDirectory "$SMPROGRAMS\${APPNAME}"
    CreateShortcut "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\${EXENAME}"


    ${If} $DESKTOP_CHECKED == 1
        CreateShortcut "$Desktop\${APPNAME}.lnk" "$INSTDIR\${EXENAME}"
    ${EndIf}

    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayIcon" "$INSTDIR\${EXENAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayVersion" "1.0.0.0"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoRepair" 1

    end_maint:
SectionEnd

Section "Uninstall"
    Delete "$INSTDIR\*.*"
    RMDir /r "$INSTDIR"

    SetShellVarContext all
    Delete "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk"
    RMDir "$SMPROGRAMS\${APPNAME}"


    Delete "$Desktop\${APPNAME}.lnk"

    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"
SectionEnd
