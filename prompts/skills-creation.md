Create agent skills file to help models and agents generate this type of xml file (schema below). The file represents and integration can can have IO inputs and outputs (CSV and/or SQL DB). The possible integration steps are:

1. Map: executes a worksheet to map values
2. Filter: executes a worksheet whose last cell will decide wether the record should be excluded from the next step or not
3. Reduce: executes a worksheet that accepts an initial state (defined in the step) and accepts a record (the current record in the flow). The new state is the final worksheet executed
4. Switch: that runs a `WorkSheet` and in the xml of the integration definition, have child nodes that represent the diffent branches to take. Each branch node has an attribute that specifies the name of the `WorkCell` in the `WorkSheet` to use as the boolean value for whether or not run a branch
5. IOInput: this step can be one of the following types:
    - CSVReader
    - SqlReader
    it yields records (one at the time) to the flow to be processed by the next step.
6. IOOutput : this step can be one of the following types:
    - CSVWriter
    - SqlWriter
    it accept a record at the time from the flow and yields the same record it received plus special status fields from the output integration.

``` xml
<?xml version="1.0" encoding="UTF-8"?>
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">

  <xs:element name="Integration">
    <xs:complexType>
      <xs:sequence>
        <xs:element name="Steps">
          <xs:complexType>
            <xs:choice maxOccurs="unbounded">
              <xs:element name="IOInput" type="IOType" />
              <xs:element name="IOOutput" type="IOType" />
              <xs:element name="Map" type="MapType" />
              <xs:element name="Filter" type="WorksheetStepType" />
              <xs:element name="Reduce" type="ReduceType" />
              <xs:element name="Switch" type="SwitchType" />
            </xs:choice>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
      <xs:attribute name="Name" type="xs:string" use="required" />
    </xs:complexType>
  </xs:element>

  <xs:complexType name="IOType">
    <xs:attribute name="Id" type="xs:string" use="required" />
    <xs:attribute name="Kind" type="xs:string" use="required" />
    <xs:attribute name="FilePath" type="xs:string" use="required" />
  </xs:complexType>

  <xs:complexType name="MapType">
    <xs:sequence>
      <xs:element name="Worksheet" type="WorksheetType" />
    </xs:sequence>
    <xs:attribute name="Id" type="xs:string" use="required" />
  </xs:complexType>

  <xs:complexType name="WorksheetStepType">
    <xs:sequence>
      <xs:element name="Worksheet" type="WorksheetType" />
    </xs:sequence>
    <xs:attribute name="Id" type="xs:string" use="required" />
  </xs:complexType>

  <xs:complexType name="ReduceType">
    <xs:sequence>
      <xs:element name="InitialStateJson" type="xs:string" />
      <xs:element name="Worksheet" type="WorksheetType" />
    </xs:sequence>
    <xs:attribute name="Id" type="xs:string" use="required" />
    <xs:attribute name="OutputField" type="xs:string" use="required" />
  </xs:complexType>

  <xs:complexType name="SwitchType">
    <xs:sequence>
      <xs:element name="Worksheet" type="WorksheetType" />
      <xs:element name="Branch" maxOccurs="unbounded">
        <xs:complexType>
          <xs:sequence>
            <xs:element name="Map" type="MapType" />
          </xs:sequence>
          <xs:attribute name="WorkCell" type="xs:string" use="required" />
        </xs:complexType>
      </xs:element>
    </xs:sequence>
    <xs:attribute name="Id" type="xs:string" use="required" />
  </xs:complexType>

  <xs:complexType name="WorksheetType">
    <xs:choice>
      <xs:sequence>
        <xs:element name="Name" type="xs:string" minOccurs="0" />
        <xs:element name="Variables" minOccurs="0">
          <xs:complexType>
            <xs:sequence>
              <xs:element name="Variable" maxOccurs="unbounded">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="Key" type="xs:string" />
                    <xs:element name="Value" type="xs:string" />
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
        <xs:element name="Cells">
          <xs:complexType>
            <xs:sequence>
              <xs:element name="WorkCell" maxOccurs="unbounded">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="Id" type="xs:string" />
                    <xs:element name="Name" type="xs:string" />
                    <xs:element name="Formula" type="xs:string" />
                    <xs:element name="Comments" type="xs:string" minOccurs="0" />
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
    </xs:choice>
    <xs:attribute name="FilePath" type="xs:string" />
  </xs:complexType>

</xs:schema>
```

the skill can call the PuppyWorkbooks.CLI.exe and pass the file path to a worksheet to validate the Worksheet runs. It could also pass the file path to an integration xml file, but that would run the integration 