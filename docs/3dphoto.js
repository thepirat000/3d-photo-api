const apiUrl = "https://photo-3d.eastus.cloudapp.azure.com/photo/p";
let dzError = false;
let dropzone;

$(document).ready(function () {
	setupDropFilesBox();
	$("#btn-go").click(e => {
		Go();
	});
});

function setupDropFilesBox() {
    $("#uploader").addClass('dropzone');
	
    dropzone = new Dropzone("#uploader", {
        url: apiUrl,
        paramName: "file",
        maxFilesize: 12, // MB
        maxFiles: 20,
        timeout: 600000,
        clickable: true,
        acceptedFiles: "image/*",
        uploadMultiple: true,
        createImageThumbnails: true,
        parallelUploads: 5,
        method: "post",
        
        dictDefaultMessage: "Drop images here or Click to upload",
        successmultiple: onFileUploadCompleted,
		
		autoProcessQueue: false,
		addRemoveLinks: true,
        
 
		
        errormultiple: function (f, errorMessage) {
            if (!dzError) {
                dzError = true;
                stopWait();
                alert("Some files cannot be processed:\n" + errorMessage);
            }
        }
    });
}


function onFileUploadCompleted(f, response) {
    stopWait();
    dropzone.removeAllFiles();
    if (response.error) {
        dzError = true;
        alert(response.error);
    } else {
        // download file
        console.log("Successful split: " + JSON.stringify(response));
        let downloadUrl = split_mp3_api + "/d?fn=" + encodeURIComponent(response.fileId);
        window.open(downloadUrl);
    }
}

function stopWait() {
}

function Go() {
	dzError = false;
    if (dropzone.getQueuedFiles().length === 0) {
		return;
	}
	
	$("#file-format").val("parameter example");
	
    dropzone.processQueue(); 
}


