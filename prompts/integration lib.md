Generate code in PuppyWorkboos.Integration class library that allows workflows to be defined using xml. An integration can have many steps of the following types:

1. Map: executes a worksheet to map values
2. Filter: executes a worksheet whose last cell will decide wether the record should be excluded from the next step or not
3. Reduce: executes a worksheet that accepts an initial state (defined in the step) and accepts a recoed (the current record in the flow). The new state is the final worksheet executed
4. IOInput: this step can be one of the following types:
    - CSVReader
    - SqlReader
    it yields records (one at the time) to the flow to be processed by the next step.
5. IOOutput : this step can be one of the following types:
    - CSVWriter
    - SqlWriter
    it accept a record at the time from the flow and yields the same record it received plus special status fields from the output integration.

For the input and output steps, you can get inspiration from C:\sources\PuppyDataMapper\src-powerfx\PuppyMapper.PowerFX\3-Services\PuppyMapper.IntegrationProviders