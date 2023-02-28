export const LINE1 = "<br />"
export const LINE2 = "<br />" + LINE1
export const MSG_ERROR_PROCESSING = "Error processing the data"
export const MSG_DATA_NO_FOUND = "Data not found"

export const MESSAGES = {
    clear: "Clearing values...",
    verify: "Verify data before Uploading.",
    processing: "Processing Data ... It may take a minute.",
    upload: "Uploading values... It may take few minutes.",
    uploadSuccess: "Upload Success.",
    uploadFailure: "Upload Failure.",
    locked: "Data Upload is currently Locked for",
    locked2: "Data Upload Locked.",
    fileSize: "The file is too big to process. <br />Create an import file with only the sheets needed for the import<br />or reduce the size of the file to less than "
}

export enum SelCal {
    Calculated, Manual, All
}

export enum Intervals {
    Daily = 1, Weekly, Monthly, Quarterly, Yearly
}

const ExpressionTypes = [
    { id: 1, name: "Expression", show: false },
    { id: 2, name: "Aggregator Expression", show: true }
] as const

const Calculated = [
    { id: 0, value: null, name: "NOT SET" },
    { id: 1, valud: true, name: "YES" },
    { id: 2, value: false, name: "NO" }
] as const


export function processError(name: string, message: string, id: any, status: unknown) {
    return {
        heading: "Error: " + name,
        message: "Error Message: " + message,
        id: "Error ID: " + id,
        status: "Error Status: " + status
    }
}
