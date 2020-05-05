# Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Bypass -Force;

Set-ExecutionPolicy Bypass -Scope Process -Force; 

# Install chocolatey
iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'));

# GIT
Write-Host "Installing GIT" -foregroundcolor "green";
choco install git -y --no-progress

# CUDA drivers
Write-Host "Installing CUDA drivers (this can take some time)" -foregroundcolor "green";
choco install cuda --ignore-checksums -y --no-progress

refreshenv

mkdir C:\GIT

# Clone project
& 'C:\Program Files\Git\bin\git.exe' clone https://github.com/vt-vl-lab/3d-photo-inpainting.git
cd 3d-photo-inpainting

# Install Conda
Write-Host "Installing miniconda3 (this can take some time)" -foregroundcolor "green";
choco install miniconda3 -y --no-progress

& 'C:\tools\miniconda3\shell\condabin\conda-hook.ps1'; 
conda activate 'C:\tools\miniconda3';

conda update -n base -c defaults conda -y

conda create -n 3DP python=3.7 anaconda -y

# Install 3d-photo
conda activate 3DP
pip install -r requirements.txt
conda install pytorch==1.4.0 torchvision==0.5.0 cudatoolkit==10.1.243 -c pytorch -y

conda deactivate 

conda deactivate 

#download models

md checkpoints
$output = "checkpoints\color-model.pth";
Start-BitsTransfer -Source https://filebox.ece.vt.edu/~jbhuang/project/3DPhoto/model/color-model.pth -Destination $output

$output = "checkpoints\depth-model.pth";
Start-BitsTransfer -Source https://filebox.ece.vt.edu/~jbhuang/project/3DPhoto/model/depth-model.pth -Destination $output

$output = "checkpoints\edge-model.pth";
Start-BitsTransfer -Source https://filebox.ece.vt.edu/~jbhuang/project/3DPhoto/model/edge-model.pth -Destination $output

$output = "MiDaS\model.pt";
Start-BitsTransfer -Source https://filebox.ece.vt.edu/~jbhuang/project/3DPhoto/model/model.pt -Destination $output

Write-Host "Done..." -foregroundcolor "green";
Write-Host "You can now run on anaconda prompt: "
Write-Host "  > conda activate 3DP"
Write-Host "  (3DP)> python main.py --config argument.yml"