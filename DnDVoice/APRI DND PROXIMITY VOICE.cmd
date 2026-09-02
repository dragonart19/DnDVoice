@echo off
setlocal

set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe"
pushd "%~dp0"
set "PROJECT_PATH=%CD%"
popd
set "PROJECT_DRIVE=P:"
set "LOG_PATH=P:\UnityOpen.log"

if not exist "%UNITY_EXE%" (
    echo Unity 6000.3.8f1 non risulta installato nel percorso previsto.
    echo Apri Unity Hub e installa Unity 6.3 LTS 6000.3.8f1.
    pause
    exit /b 1
)

if exist "%PROJECT_DRIVE%\" (
    if not exist "%PROJECT_DRIVE%\.dnd-proximity-voice-project" (
        echo L'unita %PROJECT_DRIVE% e gia utilizzata da un altro percorso.
        echo Scrivi a Codex per scegliere una lettera diversa.
        pause
        exit /b 2
    )
) else (
    subst %PROJECT_DRIVE% "%PROJECT_PATH%"
    if errorlevel 1 (
        echo Non e stato possibile creare il percorso breve %PROJECT_DRIVE%.
        pause
        exit /b 3
    )
)

echo Avvio D^&D Proximity Voice con Unity 6000.3.8f1...
echo Al primo avvio l'importazione puo richiedere alcuni minuti.
start "DND Proximity Voice" "%UNITY_EXE%" -projectPath "%PROJECT_DRIVE%\" -logFile "%LOG_PATH%"

endlocal
