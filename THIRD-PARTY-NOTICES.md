# Third-party notices

Midden includes or relies on third-party software. The dependency inventory in
[`docs/maintenance/dependency-license-inventory.md`](docs/maintenance/dependency-license-inventory.md)
records the reviewed versions, artifact scope, and sources.

Self-contained CLI archives also contain `DOTNET-LICENSE.txt` and
`DOTNET-THIRD-PARTY-NOTICES.txt` from the exact Microsoft .NET runtime pack used to build that
archive. Those files contain the complete .NET runtime redistribution terms and notices.

The published Wasm application contains the following upstream copyright notices:

| Component | Copyright notice |
|---|---|
| Ant Design Blazor | Copyright (c) .NET Foundation and Contributors |
| Bootstrap 4.3.1 | Copyright (c) 2011-2019 Twitter, Inc.; Copyright (c) 2011-2019 The Bootstrap Authors |
| Leaflet 1.7.1 | Copyright (c) 2010-2019 Vladimir Agafonkin; Copyright (c) 2010-2011 CloudMade |
| Leaflet-Geoman Free 2.20.0 | Copyright (c) 2017 Sumit Kumar |
| Leaflet.heat 0.2.0 | Copyright (c) 2014 Vladimir Agafonkin |
| Markdig | Copyright (c) 2018-2019 Alexandre Mutel |
| PSC.Blazor.Components.MarkdownEditor | Copyright (c) 2023 Enrico Rossini |
| Radzen.Blazor | Copyright (c) 2018-2026 Radzen Ltd |
| Z.Blazor.Diagrams and SvgPathProperties | Copyright (c) 2018 zHaytam |

The authoritative Leaflet, Leaflet-Geoman Free, and Leaflet.heat license texts are distributed
beside their self-hosted files under `wwwroot/lib`. Open Iconic's license texts are likewise
distributed beside its assets. Exact notices for the embedded .NET runtime are preserved in the
runtime notice named above rather than duplicated here.

## MIT-licensed components

The reviewed MIT-licensed components include Microsoft .NET libraries, Azure SDK libraries,
Ant Design Blazor, OneOf, PSC.Blazor.Components.MarkdownEditor, Radzen.Blazor,
SvgPathProperties, Z.Blazor.Diagrams, Bootstrap, Leaflet-Geoman, EasyMDE, Mermaid, and Open
Iconic icons.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Apache-2.0 components

CsvHelper is used under its Apache-2.0 option. The Google API .NET client packages are also
Apache-2.0. The complete Apache License 2.0 text is included in [`LICENSE.md`](LICENSE.md).

## BSD-2-Clause components

Markdig, Leaflet, and Leaflet.heat are distributed under the BSD 2-Clause License by their
respective copyright holders.

Redistribution and use in source and binary forms, with or without modification, are permitted
provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this list of
   conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice, this list of
   conditions and the following disclaimer in the documentation and/or other materials provided
   with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

## BSD-3-Clause component

highlight.js is distributed under the BSD 3-Clause License.

Copyright (c) 2006, Ivan Sagalaev. All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted
provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this list of
   conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice, this list of
   conditions and the following disclaimer in the documentation and/or other materials provided
   with the distribution.
3. Neither the name of the copyright holder nor the names of its contributors may be used to
   endorse or promote products derived from this software without specific prior written
   permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

## Open Iconic font

The Open Iconic font is licensed under the SIL Open Font License 1.1. Its complete license text
is distributed with the published web assets at
`Caf.Midden.Wasm/wwwroot/css/open-iconic/FONT-LICENSE`. Open Iconic icon code is covered by the
MIT terms above and by the adjacent `ICON-LICENSE` file.