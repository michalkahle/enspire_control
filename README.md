# enspire_control
`enspire_control` is a package for programmatic control of the EnSpire multimodal plate reader from PerkinElmer (formerly Wallac). The package currently only supports running fluorescence scan assays! It needs to be extended to support more than that. It works by directly modifying the database which holds all the data and settings of the instrument. This package works for me but may not work for other installations.
## Installation
* Backup the database.
* Clone the repo. 
* Create a protocol named `enspire_control`. This is the protocol which this package will modify. ![default protocol screenshot](default_protocol.png)
* Make sure the "Enspire Service Host" is running: <br>
![Enspire Service Host](services_host.png)
* Try running some examples from the included notebook.
