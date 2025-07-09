import tsTypeGen from './main.mjs';

const args = process.argv.slice(2);
const HELP_ARG_NAMES = ['--help', '-h'];
const VERSION_ARG_NAMES = ['--version', '-v'];
const SOLUTION_ARG_NAMES = ['--solution', '-s'];
const PROJECT_ARG_NAMES = ['--project', '-p'];
const CONFIG_ARG_NAMES = ['--config', '-c'];
const VERIFY_ARG_NAMES = ['--verify', '-t'];

if (getVariable(HELP_ARG_NAMES, true)) {
  help();
} else if (getVariable(VERSION_ARG_NAMES, true)) {
  version();
} else {
  main();
}

function help() {
  console.info(`Examples:
tstypegen ${SOLUTION_ARG_NAMES[0]}=path/to/file.sln ${CONFIG_ARG_NAMES[0]}=path/to/config.json
tstypegen ${PROJECT_ARG_NAMES[0]}=path/to/file.csproj ${CONFIG_ARG_NAMES[0]}=path/to/config.json
tstypegen ${PROJECT_ARG_NAMES[0]}=path/to/file.csproj ${CONFIG_ARG_NAMES[0]}=path/to/config.json ${VERIFY_ARG_NAMES[0]}

Options:
${HELP_ARG_NAMES.join(', ').padEnd(32)} Print this message.
${VERSION_ARG_NAMES.join(', ').padEnd(32)} Print the version.
${SOLUTION_ARG_NAMES.join(', ').padEnd(32)} Path to solution file.
${PROJECT_ARG_NAMES.join(', ').padEnd(32)} Path to project file.
${CONFIG_ARG_NAMES.join(', ').padEnd(32)} Path to config file.
${VERIFY_ARG_NAMES.join(', ').padEnd(32)} Verify generated types instead of generating them.`);
}

async function version() {
  // Trick TS from transpiling package.json
  const pkg = await import('../package.json' + '');
  console.info(pkg.version);
}

async function main() {
  if (args.length > 0) {
    const solutionFile = getVariable(SOLUTION_ARG_NAMES);
    const projectFile = getVariable(PROJECT_ARG_NAMES);
    const configFile = getVariable(CONFIG_ARG_NAMES);
    const verify = getVariable(VERIFY_ARG_NAMES, true);

    try {
      if (typeof solutionFile === typeof projectFile) {
        throw new Error('Choose the path to either a project or solution file.');
      }

      if (typeof configFile === 'undefined') {
        throw new Error('Enter a path to the config file.');
      }

      if (typeof solutionFile === 'string') {
        await tsTypeGen({
          solutionFile,
          configFile,
          verify,
        });
      } else if (typeof projectFile === 'string') {
        await tsTypeGen({
          projectFile,
          configFile,
          verify,
        });
      }
    } catch (e) {
      if (e instanceof Error) {
        console.error(e.message);
      } else {
        throw e;
      }
    }
  } else {
    help();
  }
}

function getVariable(names: string[], expectBool?: false): string | undefined;
function getVariable(names: string[], expectBool: true): boolean;
function getVariable(names: string[], expectBool = false) {
  for (let i = 0; i < args.length; i++) {
    const arg = args[i];

    for (const name of names) {
      if (expectBool) {
        if (arg === name) {
          return true;
        }
      } else {
        if (arg.startsWith(`${name}=`)) {
          return arg.slice(name.length + 1);
        }
        if (arg === name) {
          return args[++i];
        }
      }
    }
  }

  if (expectBool) {
    return false;
  }
}
