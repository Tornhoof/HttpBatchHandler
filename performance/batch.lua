local open = io.open
local function read_file(path)
    local file = open(path, "rb") -- r read mode and b binary mode
    if not file then return nil end
    local content = file:read "*a" -- *a or *all reads the whole file
    file:close()
    return content
end

wrk.method = "POST"
wrk.body   = read_file("payload.txt")
wrk.headers["Content-Type"] = "multipart/mixed; boundary=\"batch_45cdcaaf-774f-40c6-8a12-dbb835d3132e\""