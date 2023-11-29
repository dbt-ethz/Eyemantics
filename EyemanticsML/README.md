SAM requires ``python>=3.8``, and ``pytorch>=1.7`` and ``torchvision>=0.8``. For the latter please visit: [pytorch](https://pytorch.org/get-started/locally/).

Afterwards you can install the rest of the requiremnts via ``pip install -r requirements.txt``.

Lastly move into the ``./weights`` folder and download the weights according to the instructions.

The TCP Client with the SAM model prediction included can then be started via ``python SAM.py``.
If you have a GPU available, change the specified line in ``SAM.py`` in order to drastically speed up the mask generation.