const projectNames = [...document.querySelectorAll('td a')]
    .map(a => a.textContent.trim())
    .filter(name => name.length > 0);

const csv =
    "ProjectName\n" +
    projectNames
        .map(name => `"${name.replace(/"/g, '""')}"`)
        .join("\n");

const blob = new Blob([csv], { type: "text/csv" });

const url = URL.createObjectURL(blob);

const a = document.createElement("a");
a.href = url;
a.download = "CustomizationProjects.csv";
a.click();

URL.revokeObjectURL(url);

console.log(`Exported ${projectNames.length} projects.`);