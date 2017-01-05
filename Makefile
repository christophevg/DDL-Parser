TARGET     = main.exe
BUILD      = .build
CC         = mcs
RUN        = mono
ARGUMENTS ?=

all: run

$(BUILD)/%.exe: src/%.cs
	@mkdir -p $(BUILD)
	@$(CC) -out:$@ $<

run: $(BUILD)/$(TARGET)
	@$(RUN) $< $(ARGUMENTS)
