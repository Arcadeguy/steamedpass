# Third-party notices

`steamedpass` vendors and adapts code from two MIT-licensed open-source projects.

## UWPHook

Portions of `Steamedpass.Core` (Game Pass app discovery script and logic, the UWP
launch-activation stub, and the Steam `shortcuts.vdf` read/write/backup/restart
flow) are adapted from [UWPHook](https://github.com/BrianLima/UWPHook) by Brian Lima.

```
MIT License

Copyright (c) 2016 Brian Lima

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## VDFParser

`src/Steamedpass.Core/ThirdParty/VDFParser` is vendored unmodified from
[BrianLima/VDFParser](https://github.com/BrianLima/VDFParser) by Victor Gama,
used to read and write Steam's binary `shortcuts.vdf` format. See
`src/Steamedpass.Core/ThirdParty/VDFParser/LICENSE-VDFParser.txt` for its
license text (MIT).
