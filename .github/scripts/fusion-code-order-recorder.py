#!/usr/bin/env python3
"""Runs a Native AOT program and records the names of its methods in the order of their first execution.

Usage: fusion-code-order-recorder.py --symbols <file> --output <file> -- <program> [arguments...]

<symbols> is the debug file of the program, or the program itself when it still has its symbol table. Before the
program runs its first instruction, a breakpoint is written to the entry of every method. The first time a method is
entered, its breakpoint is removed and its name is appended to the output file, so every method stops the program once.

Linux on x86-64 only. SIGTERM and SIGINT are passed on to the program. The exit code is the one of the program.
"""
import argparse
import ctypes
import mmap
import os
import platform
import signal
import struct
import sys
import time

PTRACE_TRACEME = 0
PTRACE_POKEUSER = 6
PTRACE_CONT = 7
PTRACE_GETREGS = 12
PTRACE_SETOPTIONS = 0x4200
PTRACE_O_TRACEFORK = 0x2
PTRACE_O_TRACEVFORK = 0x4
PTRACE_O_TRACECLONE = 0x8
PTRACE_O_TRACEEXEC = 0x10
PTRACE_O_EXITKILL = 0x100000
PTRACE_EVENT_EXEC = 4
WAIT_ALL = 0x40000000

# The index of rip in user_regs_struct, which has 27 fields of 8 bytes.
RIP = 16
BREAKPOINT = 0xCC

SHT_SYMTAB = 2
STT_FUNC = 2
CODE_SECTIONS = ("__managedcode", "__unbox")

libc = ctypes.CDLL(None, use_errno=True)
libc.ptrace.argtypes = [ctypes.c_long, ctypes.c_long, ctypes.c_void_p, ctypes.c_void_p]
libc.ptrace.restype = ctypes.c_long
registers = (ctypes.c_ulong * 27)()


def ptrace(request, tid, address=0, data=0):
    if libc.ptrace(request, tid, address, data) == -1:
        error = ctypes.get_errno()
        raise OSError(error, os.strerror(error))


def read_methods(symbol_file):
    """Returns the address ranges of the managed code sections and the size and names of the methods by address."""
    with open(symbol_file, "rb") as file:
        data = mmap.mmap(file.fileno(), 0, access=mmap.ACCESS_READ)
    if data[:6] != b"\x7fELF\x02\x01":
        sys.exit(f"{symbol_file} is not a 64-bit little-endian ELF file.")

    section_offset, = struct.unpack_from("<Q", data, 0x28)
    section_size, section_count, names_index = struct.unpack_from("<HHH", data, 0x3A)
    # name, type, flags, address, offset, size, link
    sections = [
        struct.unpack_from("<IIQQQQI", data, section_offset + index * section_size)
        for index in range(section_count)
    ]

    def text(table, offset):
        start = sections[table][4] + offset
        return data[start:data.find(b"\0", start)].decode()

    code = {
        index: (section[3], section[3] + section[5])
        for index, section in enumerate(sections)
        if text(names_index, section[0]) in CODE_SECTIONS
    }
    methods = {}
    for section in sections:
        if section[1] != SHT_SYMTAB:
            continue
        for offset in range(section[4], section[4] + section[5], 24):
            name, info, _, section_index, address, size = struct.unpack_from("<IBBHQQ", data, offset)
            if info & 0xF == STT_FUNC and section_index in code:
                # Methods with identical code share one address.
                methods.setdefault(address, (size, []))[1].append(text(section[6], name))
    return sorted(code.values()), methods


def find_load_address(pid, program):
    """Returns the address at which the program file is mapped into the process."""
    with open(f"/proc/{pid}/maps") as maps:
        for line in maps:
            parts = line.split()
            if len(parts) >= 6 and os.path.realpath(parts[5]) == program and int(parts[2], 16) == 0:
                return int(parts[0].split("-")[0], 16)
    raise RuntimeError("The program is not mapped into the process.")


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--symbols", required=True, help="debug file of the program, or the program itself")
    parser.add_argument("--output", required=True, help="file that receives the method names")
    parser.add_argument("command", nargs="+", help="program and its arguments, after --")
    options = parser.parse_args()

    if sys.platform != "linux" or platform.machine() != "x86_64":
        sys.exit("The recorder runs on Linux on x86-64 only.")

    ranges, methods = read_methods(options.symbols)
    if not methods:
        sys.exit(f"{options.symbols} has no method symbols in the sections {', '.join(CODE_SECTIONS)}.")
    program = os.path.realpath(options.command[0])

    pid = os.fork()
    if pid == 0:
        libc.ptrace(PTRACE_TRACEME, 0, None, None)
        os.kill(os.getpid(), signal.SIGSTOP)
        try:
            os.execv(options.command[0], options.command)
        finally:
            os._exit(127)

    def forward(signal_number, _frame):
        try:
            os.kill(pid, signal_number)
        except ProcessLookupError:
            pass

    signal.signal(signal.SIGTERM, forward)
    signal.signal(signal.SIGINT, forward)

    os.waitpid(pid, WAIT_ALL)
    ptrace(
        PTRACE_SETOPTIONS, pid, 0,
        PTRACE_O_TRACECLONE | PTRACE_O_TRACEFORK | PTRACE_O_TRACEVFORK | PTRACE_O_TRACEEXEC | PTRACE_O_EXITKILL)
    ptrace(PTRACE_CONT, pid, 0, 0)

    memory = None
    load_address = None
    original = {}
    armed = set()
    executed = 0
    threads = {pid}
    exit_status = None

    with open(options.output, "w") as output:
        last_flush = time.monotonic()
        while exit_status is None:
            try:
                tid, status = os.waitpid(-1, WAIT_ALL)
            except ChildProcessError:
                break
            if os.WIFEXITED(status) or os.WIFSIGNALED(status):
                threads.discard(tid)
                if tid == pid:
                    exit_status = status
                continue
            if not os.WIFSTOPPED(status):
                continue

            stop_signal = os.WSTOPSIG(status)
            event = status >> 16
            deliver = 0
            if event == PTRACE_EVENT_EXEC:
                if tid == pid and load_address is None:
                    # The program is mapped and has not run yet: write the breakpoints.
                    load_address = find_load_address(pid, program)
                    memory = os.open(f"/proc/{pid}/mem", os.O_RDWR)
                    for start, end in ranges:
                        code = bytearray(os.pread(memory, end - start, load_address + start))
                        for address in methods:
                            if start <= address < end:
                                original[address] = code[address - start]
                                code[address - start] = BREAKPOINT
                        os.pwrite(memory, bytes(code), load_address + start)
                    armed = set(methods)
            elif event != 0:
                pass
            elif stop_signal == signal.SIGTRAP:
                ptrace(PTRACE_GETREGS, tid, 0, ctypes.addressof(registers))
                address = registers[RIP] - 1 - (load_address or 0)
                if load_address is not None and address in methods:
                    if address in armed:
                        armed.discard(address)
                        os.pwrite(memory, bytes([original[address]]), load_address + address)
                        for name in methods[address][1]:
                            output.write(name + "\n")
                        executed += 1
                    # Continue at the restored first instruction of the method.
                    ptrace(PTRACE_POKEUSER, tid, 8 * RIP, load_address + address)
                else:
                    deliver = stop_signal
            elif stop_signal == signal.SIGSTOP and tid not in threads:
                # The first stop of a new thread.
                pass
            else:
                deliver = stop_signal
            threads.add(tid)
            try:
                ptrace(PTRACE_CONT, tid, 0, deliver)
            except OSError:
                threads.discard(tid)
            if time.monotonic() - last_flush > 2:
                output.flush()
                last_flush = time.monotonic()

    print(f"{executed} of {len(methods)} methods were executed.", file=sys.stderr)
    if exit_status is None:
        return 1
    return os.WEXITSTATUS(exit_status) if os.WIFEXITED(exit_status) else 128 + os.WTERMSIG(exit_status)


if __name__ == "__main__":
    sys.exit(main())
